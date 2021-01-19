using CommandLine;
using CommandLine.Text;
using System.Collections.Generic;

namespace AddZoomMetadata
{
    class Options
    {
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

        [Usage(ApplicationAlias = "AddZoomMetadata")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                return new List<Example>() {
                        new Example("Process a single media entry and stop",
                                    new Options { entryId = "0_xxxxxxx" }),
                        new Example("Process all media entries after date.  Create XML output",
                                    new Options { strDate = "2020-01-01",  })
                };
            }
        }
    }
}
