using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataBaseEngine;
using DataBaseType;
using IntegrationTests.TestApi.QueryGenerator;
using ProtoBuf;
using SunflowerDB;
using TransactionManagement;

namespace IntegrationTests
{
    class TestsDebuger
    {
        private static void Main (string[] args)
        {/*
            var test1 = new Create_ShowCreate(true);
            test1.TestTest();
            test1.TestCreateCommandSynax();
            Console.ReadKey();
            */
            var test = new MultyThreadTest(true);
            test.InsertSelectTest();
            //test.MainTest();
            /*
            var test2 = new DurabilityTest(true);
            test2.DeleteDurability();
            test2.InsertDurability();
            test2.UpdateDurability();
            test2.DeleteDurabilityNoKill();
            test2.InsertDurabilityNoKill();
            test2.UpdateDurabilityNoKill();*/

            Console.ReadKey();
            Console.ReadKey();
            Console.ReadKey();
            Console.ReadKey();
            Console.ReadKey();
            Console.ReadKey();
            Console.ReadKey();
            Console.ReadKey();
        }
    }
}
