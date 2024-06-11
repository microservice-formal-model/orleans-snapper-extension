using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CheckoutsTransformer.CLI
{
    internal class Options
    {
        private const string eHelpText = "If flag is set, it transforms a checkouts database" +
            " from regular Snapper to extended Snapper. Default is not set set.";

        [Option('e',"toExtended",Required = false,Default = false,HelpText = eHelpText)]
        public bool Extended { get; set; }

        [Option('p',"path",Required = true,HelpText = "Sets path of database to perform the transformation relative to environment" +
            "variable esnapper. Required.")]
        public string Path { get; set; }

        [Option('r',"resultPath",Required = true,HelpText = "Sets path of resulting csv file relative to esnapper environment variable")]
        public string ResultPath { get; set; }
    }
}
