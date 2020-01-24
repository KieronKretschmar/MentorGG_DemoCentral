using System;
using System.Collections.Generic;
using System.Text;
using DemoCentral;
using DataBase.DatabaseClasses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.InMemory;

namespace DemoCentralTests
{
    static class DCTestsDBHelper
    {
            public static DbContextOptions<DemoCentralContext> test_config = new DbContextOptionsBuilder<DemoCentralContext>().UseInMemoryDatabase(databaseName: "DemoCentralTestDB").Options;
     
    }
}
