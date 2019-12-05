using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DemoCentral.Enumerals
{
    public enum UploadTypes : byte
    {
        UNKNOWN = 0,
        EXTENSION = 1,
        UPLOADER = 2,
        FACEITMATCHGATHERER = 3,
        MANUALUSERUPLOAD = 4,
        MANUALADMINUPLOAD = 5,
        EVENTUPLOAD = 6,
        SHARECODE = 7,
    }
}