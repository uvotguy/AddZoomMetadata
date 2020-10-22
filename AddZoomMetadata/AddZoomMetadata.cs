using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using Kaltura;
using Kaltura.Enums;
using Kaltura.Types;
using Kaltura.Request;
using Kaltura.Services;
using System.Threading;
using System.Security.Cryptography;
using System.Configuration;
using System.Xml;
using CommandLine;
using NLog;

namespace AddZoomMetadata
{
    class AddZoomMetadata
    {
        static void Main(string[] args)
        {
            // Only Kaltura service administrators have access to these.  Make sure
            // to protect them.
            int partnerId = -1;
            string secret = "";

            // See comments in App.config.
            int targetMetadataProfileId = Properties.Settings.Default.targetMetadataProfileId;
            string targetCategory = Properties.Settings.Default.targetCategory;

            bool generateXml = true;
            string entryId = "";
            DateTime startDate = DateTime.MaxValue;

            //// Save the last timestamp.  That's where we'll start the next time
            //// the program executes - unless we specify a StartDate on the command
            //// line.
            int lastTimestamp = 0;
            
            var parseResult = Parser.Default.ParseArguments<Options>(args)
                    .WithParsed(options =>
                    {
                        generateXml = options.generateXml == "true";  // Required option.

                        entryId = options.entryId;  // Empty string if option not set.
                        if (string.IsNullOrEmpty(options.strDate) == false)
                        {
                            bool success = DateTime.TryParse(options.strDate, out startDate);
                            if (success)
                            {
                                if (startDate.Year < 1970)
                                {
                                    Utilities.logInfo(LogLevel.Error, "Dates before 1970-01-01 not allowed.");
                                    Environment.Exit(1);
                                }
                            }
                            else
                            {
                                Utilities.logInfo(LogLevel.Error, "Invalid date format:  " + options.strDate);
                                Environment.Exit(2);
                            };
                        } else
                        {
                            // Start date not specified.
                            startDate = DateTime.MaxValue;
                        }

                        if (string.IsNullOrEmpty(options.apiSecret) == false)
                        {
                            secret = options.apiSecret;
                            Properties.Settings.Default.kalturaAdminSecret = secret;
                            Properties.Settings.Default.Save();
                            Utilities.logInfo(LogLevel.Info, "API Secret saved in user config file.");
                        } else
                        {
                            secret = Properties.Settings.Default.kalturaAdminSecret;
                        }

                        if (options.partnerId != 0)
                        {
                            partnerId = options.partnerId;
                            Properties.Settings.Default.kalturaPartnerId = partnerId;
                            Properties.Settings.Default.Save();
                            Utilities.logInfo(LogLevel.Info, "Partner ID saved in user config file.");
                        } else
                        {
                            partnerId = Properties.Settings.Default.kalturaPartnerId;
                        }

                        if (options.metadataProfileId != 0)
                        {
                            targetMetadataProfileId = options.metadataProfileId;
                            Properties.Settings.Default.targetMetadataProfileId = targetMetadataProfileId;
                            Properties.Settings.Default.Save();
                            Utilities.logInfo(LogLevel.Info, "Metadata Profile ID saved in user config file.");
                        }
                        else
                        {
                            targetMetadataProfileId = Properties.Settings.Default.targetMetadataProfileId;
                        }
                    })
                    .WithNotParsed(options =>
                    {
                        Environment.Exit(3);
                    });

            if (generateXml)
            {
                Utilities.logInfo(LogLevel.Info, "Generate XML output");
            } else
            {
                Utilities.logInfo(LogLevel.Info, "Using API mode (no XML output)");
            }

            // entryId and startDate options are mutually exclusive.  The command line
            // parser will prevent them from both being specified.
            if (string.IsNullOrEmpty(entryId) == false)
            {
                string msg = string.Format("Processing a single media entry:  Id={0};", entryId);
                Utilities.logInfo(LogLevel.Info, msg);
            } else
            {
                // Not in single entry mode
                if (startDate == DateTime.MaxValue)
                {
                    // A start date has not been specified.  Use value in the app
                    // configuration file.
                    lastTimestamp = Properties.Settings.Default.startTimestamp;
                    startDate = Utilities.unixToDotNetTime(lastTimestamp);
                } else
                {
                    lastTimestamp = Utilities.dotNetToUnixTime(startDate);
                }
            }

            KalturaUtilities kalUtil = null;
            try
            {
                kalUtil = new KalturaUtilities(partnerId, secret);
            }
            catch
            {
                Utilities.logInfo(LogLevel.Error, "Kaltura client initialization failed.  Did you set the Partner ID and Admin Secret in your user config file?");
                Environment.Exit(4);
            }

            //// If we're in "generateXml" mode, add the XML for a given entry
            //// to a list.  When we're done with each page, create an XML format
            //// bulk upload file.
            List<string> lstXml = new List<string>();
            int pageNum = 0;
            int numPages = 0;
            string dateStr = DateTime.Today.ToString("yyyy-MM-dd");

            FilterPager pager = new FilterPager()
            {
                PageIndex = 1,
                PageSize = 500
            };

            MediaEntryFilter mediaFilter = new MediaEntryFilter();
            if (string.IsNullOrEmpty(entryId) == false)
            {
                mediaFilter.IdEqual = entryId;
                string msg = string.Format("Targeting a single entry:  {0}.", entryId);
                Utilities.logInfo(LogLevel.Info, msg);
            }
            else
            {
                string msg = string.Format("Processing starts at {0} ({1})",
                                           Utilities.unixToDotNetTime(lastTimestamp),
                                           lastTimestamp);
                Utilities.logInfo(LogLevel.Info, msg);

                mediaFilter.OrderBy = MediaEntryOrderBy.CREATED_AT_ASC;
                mediaFilter.CreatedAtGreaterThanOrEqual = lastTimestamp;

                if (targetCategory.Length > 0)
                {
                    mediaFilter.CategoriesFullNameIn = targetCategory;
                    msg = string.Format("Targeting entries in category:  {0}.", targetCategory);
                    Utilities.logInfo(LogLevel.Info, msg);
                }
            }

            if (generateXml)
            {
                Utilities.logInfo(LogLevel.Info, "Creating XML output.");
            }
            else
            {
                Utilities.logInfo(LogLevel.Info, "API calls done on the fly (no XML).");
            }

            MetadataFilter metaFilt = new MetadataFilter()
            {
                MetadataObjectTypeEqual = MetadataObjectType.ENTRY,
                MetadataProfileIdEqual = targetMetadataProfileId
            };

            try
            {
                bool donePaging = false;
                string lastEntryId = "";
                while (!donePaging)
                {
                    var result = MediaService.List(mediaFilter, pager)
                                             .ExecuteAndWaitForResponse(kalUtil.client);
                    if (result.Objects.Count == 0)
                    {
                        donePaging = true;
                    }
                    else
                    {
                        lstXml.Clear();
                        Utilities.logInfo(LogLevel.Info, "=======================================================  " + Utilities.unixToDotNetTime(lastTimestamp).ToString());
                        foreach (MediaEntry entr in result.Objects)
                        {
                            try
                            {
                                string tagXml = kalUtil.addZoomTag(entr, generateXml);
                                string metaXml = kalUtil.addCustomMetadata(metaFilt, entr, generateXml, targetMetadataProfileId);

                                if (generateXml)
                                {
                                    if ((string.IsNullOrEmpty(tagXml) == false) ||
                                        (string.IsNullOrEmpty(metaXml) == false))
                                    {
                                        string xml = "<item><action>update</action ><entryId>" +
                                                     entr.Id + "</entryId>" +
                                                     tagXml + metaXml + "</item>";
                                        lstXml.Add(xml);
                                    }
                                }

                                string msg = string.Format("{0}:", entr.Id);
                                Utilities.logInfo(LogLevel.Info, msg);
                            } catch (Exception ex)
                            {
                                Utilities.logInfo(LogLevel.Error, Utilities.formatExceptionString(ex));
                            }

                            lastEntryId = entr.Id;
                            lastTimestamp = entr.CreatedAt;
                        } // end foreach item in page of media entries.

                        if (lstXml.Count > 0)
                        {
                            // Write a page worth of XML data to a file.
                            if (numPages == 0)
                            {
                                // The call to MediaService.List() will return an
                                // estimate of the total number of media entries to be
                                // processed.  It will probably change, but it's good 
                                // enough for file naming.
                                numPages = (result.TotalCount / pager.PageSize) + 1;
                            }
                            pageNum++;
                            Utilities.writeXmlFile(dateStr, pageNum, numPages, lstXml);
                        }
                    } // page not empty

                    if ((result.Objects.Count == 1) && (lastEntryId == result.Objects[0].Id))
                    {
                        // Only the very last call should ever return a single object.
                        donePaging = true;
                    }
                    mediaFilter.CreatedAtGreaterThanOrEqual = lastTimestamp;
                } // while paging
            }
            catch (Exception ex)
            {
                string msg = System.String.Format("The AddZoomTag task encountered the following exception and exited.\n{0}",
                                                  Utilities.formatExceptionString(ex));
                Utilities.logInfo(LogLevel.Error, msg);

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
                    Utilities.logInfo(LogLevel.Info, "Last timestamp saved in user config file.");
                }
            }
            Utilities.logInfo(LogLevel.Info, "done.");
        } // Main()
    }  // class AddZoomMetadata
} // namespace
