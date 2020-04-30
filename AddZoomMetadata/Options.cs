using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;

namespace AddZoomMetadata
{
    class Options
    {
        [Value(0, Required = true)]
        [Option('x', "generate-xml",
                Required = true,
                HelpText = "Generate XML output for bulk upload (true), or issue API calls on the fly (false)."
                )]
        public string generateXml { get; set; }

        [Option('e', "entry-id",
                SetName = "singleEntry",
                Required = false,
                HelpText = "Process a single media entry.")]
        public string entryId { get; set; }

        [Option('d', "start-date",
                SetName = "multipleEntry",
                Required = false,
                HelpText = "Process all media entries created after date")
                ]
        public string strDate { get; set; }

        [Option('s', "api-secret",
                Required = false,
                HelpText = "Update / set the Kaltura API Admin Secret in your app setting file.")]
        public string apiSecret { get; set; }

        [Option('p', "partner-id",
                Required = false,
                HelpText = "Update / set the Kaltura Partner ID in your app setting file.")]
        public int partnerId { get; set; }

        [Option('m', "metadata-profile-id",
                Required = false,
                HelpText = "Update / set the ID of the custom data schema you are targeting.")]
        public int metadataProfileId { get; set; }

        [Usage(ApplicationAlias = "AddZoomMetadata")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                return new List<Example>() {
                        new Example("Process a single media entry and stop",
                                    new Options { generateXml="false", entryId = "0_xxxxxxx" }),
                        new Example("Process all media entries after date.  Create XML output",
                                    new Options { generateXml="true", strDate = "2020-01-01",  })
                };
            }
        }
    }
}
