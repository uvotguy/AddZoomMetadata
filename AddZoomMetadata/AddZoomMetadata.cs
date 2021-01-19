using CommandLine;
using Kaltura;
using Kaltura.Enums;
using Kaltura.Types;
using NLog;
using System;
using System.Collections.Generic;

namespace AddZoomMetadata
{
    class AddZoomMetadata
    {
        static void Main(string[] args)
        {
            Logger logger = NLog.LogManager.GetCurrentClassLogger();

            // See comments in App.config.
            string targetCategory = Properties.Settings.Default.targetCategory;

            string entryId = "";
            DateTime startDate = DateTime.MaxValue;

            //// Save the last timestamp.  That's where we'll start the next time
            //// the program executes - unless we specify a StartDate on the command
            //// line.
            int lastTimestamp = 0;

            var parseResult = Parser.Default.ParseArguments<Options>(args)
                    .WithParsed(options =>
                    {
                        entryId = options.entryId;  // Empty string if option not set.
                        if (string.IsNullOrEmpty(options.strDate) == false)
                        {
                            bool success = DateTime.TryParse(options.strDate, out startDate);
                            if (success)
                            {
                                if (startDate.Year < 1970)
                                {
                                    logger.Error("Dates before 1970-01-01 not allowed.");
                                    Environment.Exit(1);
                                }
                            }
                            else
                            {
                                logger.Error("Invalid date format:  " + options.strDate);
                                Environment.Exit(2);
                            };
                        }
                        else
                        {
                            // Start date not specified.
                            startDate = DateTime.MaxValue;
                        }
                    })
                    .WithNotParsed(options =>
                    {
                        Environment.Exit(3);
                    });

            // entryId and startDate options are mutually exclusive.  The command line
            // parser will prevent them from both being specified.
            if (string.IsNullOrEmpty(entryId) == false)
            {
                logger.Info($"Processing a single media entry:  Id={entryId};");
            }
            else
            {
                // Not in single entry mode
                if (startDate == DateTime.MaxValue)
                {
                    // A start date has not been specified.  Use value in the app
                    // configuration file.
                    lastTimestamp = Properties.Settings.Default.startTimestamp;
                    if (lastTimestamp == 0)
                    {
                        lastTimestamp = (int)new DateTime(2018, 1, 5).Ticks;
                    }
                    startDate = KochKalturaUtilities.util.unixToDotNetTime(lastTimestamp);
                }
                else
                {
                    lastTimestamp = KochKalturaUtilities.util.dotNetToUnixTime(startDate);
                }
            }

            Kaltura.Client client = null;
            try
            {
                client = KochKalturaUtilities.ClientUtilities.createKalturaClient();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Kaltura client initialization failed.");
                Environment.Exit(4);
            }

            int targetMetadataProfileId = -1;
            string metadataProfileName = "PSU Custom Metadata";
            try
            {
                targetMetadataProfileId = KochKalturaUtilities.MetadataUtilities.getProfileIdByName(client, metadataProfileName);
                logger.Info($"Target metadata profile found.  Name={metadataProfileName};Id={targetMetadataProfileId};");
            }
            catch (ApplicationException ex)
            {
                logger.Error(ex, $"ERROR:  {ex.Message})");
                Environment.Exit(5);
            }

            int totalEntries = 0;
            int entriesSoFar = 0;

            FilterPager pager = new FilterPager()
            {
                PageIndex = 1,
                PageSize = 500
            };

            MediaEntryFilter mediaFilter = new MediaEntryFilter();
            if (string.IsNullOrEmpty(entryId) == false)
            {
                mediaFilter.IdEqual = entryId;
                logger.Info($"Targeting a single entry:  {entryId}");
            }
            else
            {
                logger.Info($"Processing starts at {KochKalturaUtilities.util.unixToDotNetTime(lastTimestamp)} ({lastTimestamp})");

                mediaFilter.OrderBy = MediaEntryOrderBy.CREATED_AT_ASC;
                mediaFilter.CreatedAtGreaterThanOrEqual = lastTimestamp;

                if (targetCategory.Length > 0)
                {
                    mediaFilter.CategoriesFullNameIn = targetCategory;
                    logger.Info($"Targeting entries in category:  {targetCategory}.");
                }
            }

            // Process all media entries (filtered) that are in the target category (Zoom Recordings).
            try
            {
                MetadataFilter metaFilt = new MetadataFilter()
                {
                    MetadataObjectTypeEqual = MetadataObjectType.ENTRY,
                    MetadataProfileIdEqual = targetMetadataProfileId
                };

                bool donePaging = false;
                string lastEntryId = "";
                while (!donePaging)
                {
                    List<MediaEntry> pageOfEntries = KochKalturaUtilities.MediaUtilities.getPageOfEntries(
                                                                         client,
                                                                         mediaFilter,
                                                                         pager,
                                                                         ref totalEntries,
                                                                         entriesSoFar,
                                                                         ref lastTimestamp,
                                                                         ref donePaging);
                    if (pageOfEntries == null)
                    {
                        donePaging = true;
                        continue;
                    }
                    logger.Info("=======================================================" + KochKalturaUtilities.util.unixToDotNetTime(lastTimestamp).ToString("yyyy-MM-dd HH:mm tt"));
                    foreach (MediaEntry entr in pageOfEntries)
                    {
                        try
                        {
                            logger.Info($"{entr.Id}:");
                            KochKalturaUtilities.MetadataUtilities.addZoomTag(client, entr);
                            KochKalturaUtilities.MetadataUtilities.addCustomMetadata(client,
                                                                                     metaFilt,
                                                                                     entr,
                                                                                     targetMetadataProfileId);
                        }
                        catch (Exception ex)
                        {
                            logger.Error(KochKalturaUtilities.util.formatExceptionString(ex));
                        }

                        lastEntryId = entr.Id;
                        lastTimestamp = entr.CreatedAt;
                    } // end foreach item in page of media entries.

                    mediaFilter.CreatedAtGreaterThanOrEqual = lastTimestamp;
                } // while paging
            }
            catch (Exception ex)
            {
                logger.Error("The AddZoomTag task encountered the following exception and exited.\n{0}",
                             KochKalturaUtilities.util.formatExceptionString(ex));

                // In case of an exception, bump the last timestamp by one.  Hopefully we'll
                // get past the offending entry if that's the problem.
                lastTimestamp++;
            }
            finally
            {
                // Unless we're in "process a single entry" mode, save the last timestamp.
                // That's where we'll start on the next run.
                if (string.IsNullOrEmpty(entryId))
                {
                    Properties.Settings.Default.startTimestamp = lastTimestamp;
                    Properties.Settings.Default.Save();
                    logger.Info("Last timestamp saved in user config file.");
                }
            }
            logger.Info("done processing new media in the Zoom Recordings category.");
            logger.Info("");

            // =================================================================================================
            // =================================================================================================
            // =================================================================================================
            // Process all media entries (filtered) that are in the target category (Zoom Recordings).
            try
            {
                logger.Info("Processing Zoom Marketplace generated media files.");
                logger.Info("Processing starts at {0}", startDate.ToString("yyyy-MM-dd HH:mm tt"));

                MetadataFilter metaFilt = new MetadataFilter()
                {
                    MetadataObjectTypeEqual = MetadataObjectType.ENTRY,
                    MetadataProfileIdEqual = targetMetadataProfileId
                };

                lastTimestamp = Properties.Settings.Default.marketplaceTimestamp;
                if (lastTimestamp == 0)
                {
                    // This is when the Marketplace App was installed.
                    startDate = new DateTime(2021, 1, 5);
                    lastTimestamp = KochKalturaUtilities.util.dotNetToUnixTime(startDate);
                }
                else
                {
                    startDate = KochKalturaUtilities.util.unixToDotNetTime(lastTimestamp);
                }

                mediaFilter = new MediaEntryFilter
                {
                    CreatedAtGreaterThanOrEqual = lastTimestamp,
                    OrderBy = MediaEntryOrderBy.CREATED_AT_ASC
                    // AdminTagsLike = "zoomentries"
                };

                int targetCategoryId = KochKalturaUtilities.CategoryUtilities.getCategoryByFullname(client, targetCategory);
                bool donePaging = false;
                string lastEntryId = "";
                while (!donePaging)
                {
                    List<MediaEntry> pageOfEntries = KochKalturaUtilities.MediaUtilities.getPageOfEntries(
                                                                         client,
                                                                         mediaFilter,
                                                                         pager,
                                                                         ref totalEntries,
                                                                         entriesSoFar,
                                                                         ref lastTimestamp,
                                                                         ref donePaging);
                    if (pageOfEntries == null)
                    {
                        donePaging = true;
                        continue;
                    }
                    logger.Info("=======================================================" + KochKalturaUtilities.util.unixToDotNetTime(lastTimestamp).ToString("yyyy-MM-dd HH:mm tt"));
                    foreach (MediaEntry entr in pageOfEntries)
                    {
                        if ((entr.AdminTags == null) || (!entr.AdminTags.Contains("zoomentry"))) continue;

                        try
                        {
                            logger.Info($"{entr.Id}:");
                            try
                            {
                                KochKalturaUtilities.CategoryEntryUtilities.addEntryToCategory(client, entr.Id, targetCategoryId);
                            }
                            catch (Kaltura.APIException ex)
                            {
                                if (ex.Code == APIException.CATEGORY_ENTRY_ALREADY_EXISTS)
                                {
                                    // ignore
                                    //logger.Info("\tEntry already in Zoom Recordings category");
                                }
                                else
                                {
                                    throw;
                                }
                            }
                            KochKalturaUtilities.MetadataUtilities.addZoomTag(client, entr);
                            KochKalturaUtilities.MetadataUtilities.addCustomMetadata(client,
                                                                                     metaFilt,
                                                                                     entr,
                                                                                     targetMetadataProfileId);
                        }
                        catch (Exception ex)
                        {
                            logger.Error(KochKalturaUtilities.util.formatExceptionString(ex));
                        }

                        lastEntryId = entr.Id;
                        lastTimestamp = entr.CreatedAt;
                    } // end foreach item in page of media entries.

                    mediaFilter.CreatedAtGreaterThanOrEqual = lastTimestamp;
                } // while paging
            }
            catch (Exception ex)
            {
                logger.Error("The AddZoomTag task encountered the following exception and exited.\n{0}",
                             KochKalturaUtilities.util.formatExceptionString(ex));

                // In case of an exception, bump the last timestamp by one.  Hopefully we'll
                // get past the offending entry if that's the problem.
                lastTimestamp++;
            }
            finally
            {
                // Unless we're in "process a single entry" mode, save the last timestamp.
                // That's where we'll start on the next run.
                if (string.IsNullOrEmpty(entryId))
                {
                    Properties.Settings.Default.marketplaceTimestamp = lastTimestamp + 1;
                    Properties.Settings.Default.Save();
                    logger.Info("Last marketplace timestamp saved in user config file.");
                }
            }
            logger.Info("....  done");

        } // Main()
    }  // class AddZoomMetadata
} // namespace
