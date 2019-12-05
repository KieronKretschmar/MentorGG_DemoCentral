using System;
using System.Collections.Generic;

namespace DemoCentral.DatabaseClasses
{
    public partial class Migrationhistory
    {
        public string MigrationId { get; set; }
        public string ContextKey { get; set; }
        public byte[] Model { get; set; }
        public string ProductVersion { get; set; }
    }
}
