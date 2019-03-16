using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BingSearcher
{
    [Command("Config", Description = "Account search configuration")]
    internal class SearchConfig
    {
        public bool Disabled { get; } = false;
        public int NumSearches { get; } = 30;
        public int SearchDelay { get; } = 65;
        public bool ClickLinks { get; }
    }
}