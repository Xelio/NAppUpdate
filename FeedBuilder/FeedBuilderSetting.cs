using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace FeedBuilder
{
    public class FeedBuilderSetting
    {
        public string BaseURL { get; set; }

        public string FeedXML { get; set; }

        [DefaultValue(".")]
        public string OutputFolder { get; set; }

        [DefaultValue(true)]
        public bool CleanUp { get; set; }

        [DefaultValue(false)]
        public bool CompareDate { get; set; }

        [DefaultValue(true)]
        public bool CompareHash { get; set; }

        [DefaultValue(false)]
        public bool CompareSize { get; set; }

        [DefaultValue(true)]
        public bool CompareVersion { get; set; }

        [DefaultValue(true)]
        public bool CopyFiles { get; set; }

        [DefaultValue(true)]
        public bool IgnoreDebugSymbols { get; set; }

        [DefaultValue(true)]
        public bool IgnoreVsHosting { get; set; }

        public List<string> IncludeFiles { get; set; }
    }
}
