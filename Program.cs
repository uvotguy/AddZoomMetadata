using CommandLine;
using Kaltura.Enums;
using Kaltura.Types;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NLog;
using NLog.Extensions.Logging;
using System.Dynamic;

namespace AddZoomMetadata
{
    class Program
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private static List<MediaEntry> lstEntries = new();
        private static int lastTimestamp = 0;  // Keep track of the last timestamp processed.  At the end, save
                                               // it to the config file.  Kaltura uses UNIX time.
        public static IConfiguration config;

        public static DateTime startDate = DateTime.MinValue;

        // The Kaltura API does not like filtering on times significantly before the start
        // date of your Kaltura service.  Figure out the date of the first entry in your
        // instance and add it to the config file.
        public static DateTime serviceStartDate = DateTime.MinValue;

        static void Main(string[] args)
        {
            config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", false)
                .AddEnvironmentVariables()
                .Build();

            serviceStartDate = DateTime.Parse(config["Settings:serviceStartDate"]);
            LogManager.Configuration = new NLogLoggingConfiguration(config.GetSection("NLog"));
 
            logger.Info("Program start");

            // Kaltura API credentials are read from Environment variables.
            int partnerId;
            try
            {
                partnerId = int.Parse(config.GetValue<string>("KALTURA_PID"));
            } catch
            {
                throw new ApplicationException("Error getting environment variable:  KALTURA_PID");
            }

            string kalturaSecret = config.GetValue<string>("KALTURA_SECRET");

            if (string.IsNullOrEmpty(kalturaSecret))
            {
                throw new ApplicationException("Error getting environment variable:  KALTURA_SECRET");
            }

            string uid = Environment.UserName;

            if (args.Count() > 0)
            {
                ParserResult<Options> opt = Parser.Default.ParseArguments<Options>(args)
                                                          .WithParsed(parseOptions)
                                                          .WithNotParsed(handleParseError);
            } else
            {
                // Start date not specifiec on command line.  Read last time saved in config file.
                lastTimestamp = int.Parse(config["Settings:startTimestamp"]);
            }

            // See comment on serviceStartDate above.
            lastTimestamp = Math.Max(lastTimestamp, KochKalturaUtilities.util.dotNetToUnixTime(serviceStartDate));

            Kaltura.Client client = KochKalturaUtilities
                                    .ClientUtilities
                                    .createKalturaClient(partnerId, kalturaSecret, 86400, uid, logger);

            // All Zoom recordings will be added to this category if they're not already there.
            string targetCategory = config["Settings:targetCategory"];
            int targetCategoryId = -1;
            try
            {
                targetCategoryId = KochKalturaUtilities.CategoryUtilities.getCategoryByFullname(client, targetCategory);
            }
            catch (Kaltura.APIException ex)
            {
                logger.Error($"Failed to get the ID of the target category:  {targetCategory}." +
                             $"  Error={KochKalturaUtilities.util.formatExceptionString(ex)};");
                Environment.Exit(5);
            }

            string metadataProfileName = config["Settings:metadataProfileName"];
            int targetMetadataProfileId = -1;
            try
            {
                targetMetadataProfileId = KochKalturaUtilities.MetadataUtilities.getProfileIdByName(client, metadataProfileName);
            }
            catch (ApplicationException ex)
            {
                logger.Error(ex, $"Failed to get target metadata profile ID:  Name={metadataProfileName};" +
                                 $"Error={KochKalturaUtilities.util.formatExceptionString(ex)};");
                Environment.Exit(6);
            }

            FilterPager pager = new FilterPager()
            {
                PageIndex = 1,
                PageSize = 500
            };

            MetadataFilter metaFilt = new MetadataFilter()
            {
                MetadataObjectTypeEqual = MetadataObjectType.ENTRY,
                MetadataProfileIdEqual = targetMetadataProfileId
            };

            // Setup done.  Processing starts here ...

            int totalEntries = 0;
            int entriesSoFar = 0;
            bool donePaging = false;

            logger.Info($"Processing starts at {KochKalturaUtilities.util.unixToDotNetTime(lastTimestamp)} ({lastTimestamp})");

            // Process all media entries (filtered) that have the "zoomentry" admin tag.
            try
            {
                MediaEntryFilter mediaFilter = new MediaEntryFilter();
                mediaFilter.OrderBy = MediaEntryOrderBy.CREATED_AT_ASC;
                mediaFilter.CreatedAtGreaterThanOrEqual = lastTimestamp;
                mediaFilter.AdminTagsLike = "zoomentry";

                while (!donePaging)
                {
                    List<MediaEntry> pageOfEntries = KochKalturaUtilities.MediaUtilities.getPageOfEntries(
                                                                            client,
                                                                            logger,
                                                                            ref mediaFilter,
                                                                            ref pager,
                                                                            ref totalEntries,
                                                                            ref entriesSoFar,
                                                                            ref lastTimestamp,
                                                                            ref donePaging);
                    if (pageOfEntries == null)
                    {
                        donePaging = true;
                        continue;
                    }

                    logger.Info("================== Page of Media Entries ==================" + KochKalturaUtilities.util.unixToDotNetTime(lastTimestamp).ToString("yyyy-MM-dd HH:mm tt"));
                    foreach (MediaEntry entr in pageOfEntries)
                    {
                        try
                        {
                            logger.Info($"{entr.Id}:");
                            KochKalturaUtilities.MetadataUtilities.addZoomTag(client, logger, entr);
                            KochKalturaUtilities.MetadataUtilities.addCustomMetadata(client,
                                                                                     logger,
                                                                                     metaFilt,
                                                                                     entr,
                                                                                     targetMetadataProfileId);
                            KochKalturaUtilities.CategoryEntryUtilities.addEntryToCategory(client,
                                                                                           logger,
                                                                                           entr.Id,
                                                                                           targetCategoryId);
                        }
                        catch (Exception ex)
                        {
                            logger.Error(KochKalturaUtilities.util.formatExceptionString(ex));
                        }
                        lastTimestamp = entr.CreatedAt;
                    } // end foreach item in page of media entries.
                } // while paging
            }
            catch (Kaltura.APIException ex)
            {
                logger.Error(ex, "The AddZoomTag task encountered the following exception and exited.\n{0}",
                                KochKalturaUtilities.util.formatExceptionString(ex));

                // In case of an exception, bump the last timestamp by one.  Hopefully we'll
                // get past the offending entry next time the code is run.
                lastTimestamp++;
            } catch (Exception ex)
            {
                logger.Error(ex, "Unhandled exception:  Exception={0};", KochKalturaUtilities.util.formatExceptionString(ex));
                throw;
            }
            finally
            {
                saveSettings();
                logger.Info("Program exit");
                LogManager.Shutdown();
            }
        }

        static void saveSettings()
        {
            var appSettingsPath = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "appsettings.json");
            var json = File.ReadAllText(appSettingsPath);

            JsonSerializerSettings? jsonSettings = new();
            jsonSettings.Converters.Add(new ExpandoObjectConverter());
            jsonSettings.Converters.Add(new StringEnumConverter());

            dynamic newconfig = JsonConvert.DeserializeObject<ExpandoObject>(json, jsonSettings);
            if (newconfig != null)
            {
                newconfig.Settings.LastCreatedTime = lastTimestamp;
                var newJson = JsonConvert.SerializeObject(newconfig, Formatting.Indented, jsonSettings);

                File.WriteAllText(appSettingsPath, newJson);
                logger.Info($"last timestamp saved to {appSettingsPath}:  {lastTimestamp}");
            }
        }

        public static void parseOptions(Options opts)
        {
            if (string.IsNullOrEmpty(opts.strDate) == false)
            {
                try
                {
                    startDate = DateTime.Parse(opts.strDate);
                    if (startDate < serviceStartDate)
                    {
                        logger.Error($"Dates before {serviceStartDate} not supported.");
                        Environment.Exit(1);
                    }
                }
                catch
                {
                    logger.Error($"Invalid date format supplied on command line:  { opts.strDate}");
                    Environment.Exit(2);
                }
                lastTimestamp = KochKalturaUtilities.util.dotNetToUnixTime(startDate);
            }
            else
            {
                // Start date not specified on command line.  Read the last timestamp saved
                // in the config file.
                lastTimestamp = int.Parse(config["Settings:startTimestamp"]);
            }
        }

        static void handleParseError(IEnumerable<Error> errors)
        {
            foreach (UnknownOptionError err in errors)
            {
                logger.Error($"{err.ToString()}.  Unknown tag={err.Token};");
            }
            Environment.Exit(1);
        }
    }
}