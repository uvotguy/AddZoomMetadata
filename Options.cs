using CommandLine;
using CommandLine.Text;
using System.Collections.Generic;

namespace AddZoomMetadata
{
    class Options
    {
        [Option('d', "start-date",
                SetName = "multipleEntry",
                Required = false,
                HelpText = "Process all media entries created after date")
                ]
        public string strDate { get; set; }

        public Options()
        {
            strDate = DateTime.MinValue.ToShortDateString();
        }
        [Usage(ApplicationAlias = "AddZoomMetadata")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                return new List<Example>() {
                         new Example("Process all media entries after date.  Create XML output",
                                    new Options { strDate = "2020-01-01",  })
                };
            }
        }
    }
}
