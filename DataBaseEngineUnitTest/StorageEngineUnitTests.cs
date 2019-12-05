﻿using System.Collections.Generic;
using System.IO;

using DataBaseEngine;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DataBaseType;
using StorageEngine;

using ZeroFormatter;

namespace DataBaseEngineUnitTest
{
    [TestClass]
    public class StorageEngineUnitTests
    {
        DataStorageInFiles dataStorage;
        const int blockSize = 4096;
        [TestInitialize]
        public void TestInitialize()
        {


        }

        [TestCleanup]
        public void TestCleanup()
        {

        }
        [TestMethod]
        public void AddTableTest()
        {   
            const string testPath = "StorageTestAddTableTest";
            if (Directory.Exists(testPath))
            {
                Directory.Delete(testPath, true);
            }
            dataStorage = new DataStorageInFiles(testPath, blockSize);


            var tableName = new List<string>() { "Table1" };
            var columns = new Dictionary<string, Column> {
                { "Name",new Column(new List<string>(){"Name"}, DataType.CHAR, 25, new List<string>(), NullSpecOpt.Null) },
                { "Soname",new Column(new List<string>(){"Soname"}, DataType.CHAR, 30, new List<string>(), NullSpecOpt.Null) },
                { "Age",new Column(new List<string>(){"Age"}, DataType.INT, 0, new List<string>(), NullSpecOpt.Null) },
                { "Rating",new Column(new List<string>(){"Rating" }, DataType.DOUBLE, 0, new List<string>(), NullSpecOpt.Null) },
                };
            var table = new Table(new TableMetaInf(tableName) { ColumnPool = columns });
            var result = dataStorage.AddTable(table);
            Assert.AreEqual(result.State, OperationExecutionState.performed);

            var resultCont = dataStorage.ContainsTable(table.TableMetaInf.Name);
            Assert.AreEqual(resultCont.State, OperationExecutionState.performed);
            Assert.AreEqual(resultCont.Result, true);

            resultCont = dataStorage.ContainsTable(new List<string>() { "radomTable" });
            Assert.AreEqual(resultCont.State, OperationExecutionState.failed);
            Assert.AreEqual(resultCont.Result, false);

            var resultTable = dataStorage.LoadTable(table.TableMetaInf.Name);
            Assert.AreEqual(resultTable.State, OperationExecutionState.performed);
            Assert.AreEqual(resultTable.Result.TableMetaInf.Name[0], table.TableMetaInf.Name[0]);
            foreach (var col in resultTable.Result.TableMetaInf.ColumnPool)
            {
                Assert.AreEqual(columns[col.Key].Name[0], col.Value.Name[0]);
                Assert.AreEqual(columns[col.Key].DataType, col.Value.DataType);
                Assert.AreEqual(columns[col.Key].DataParam, col.Value.DataParam);
            }
        }
        [TestMethod]
        public void InsertRowsTest()
        {
            const string testPath = "StorageTestInsertRowsTest";
            if (Directory.Exists(testPath))
            {
                Directory.Delete(testPath, true);
            }
            dataStorage = new DataStorageInFiles(testPath, 805);

            var tableName = new List<string>() { "Table1" };
            var columns = new Dictionary<string, Column> {
                { "Name",new Column(new List<string>(){"Name"}, DataType.CHAR, 25, new List<string>(), NullSpecOpt.Null) },
                { "Soname",new Column(new List<string>(){"Soname"}, DataType.CHAR, 30, new List<string>(), NullSpecOpt.Null) },
                { "Age",new Column(new List<string>(){"Age"}, DataType.INT, 0, new List<string>(), NullSpecOpt.Null) },
                { "Rating",new Column(new List<string>(){"Rating" }, DataType.DOUBLE, 0, new List<string>(), NullSpecOpt.Null) },
                };
            var table = new Table(new TableMetaInf(tableName) { ColumnPool = columns });
            var result = dataStorage.AddTable(table);
            Assert.AreEqual(result.State, OperationExecutionState.performed);

            //var row1 = table.CreateDefaultRow();
            //for (int i = 0; i < 10; ++i)
            //{
            //    Assert.AreEqual(row1.State, OperationExecutionState.performed);
            //    dataStorage.InsertRow(tableName, row1.Result);
            //}
            var count = 0;
            var row2 = table.CreateRowFormStr(new string[] { "Ivan", "IvanovIvanovIvanov", "23", "44.345" });
            for (var i = 0; i < 10; ++i)
            { 
                Assert.AreEqual(row2.State, OperationExecutionState.performed);
                dataStorage.InsertRow(tableName, row2.Result);
                count++;
            }

            var resultTable = dataStorage.LoadTable(table.TableMetaInf.Name);
            Assert.AreEqual(resultTable.State, OperationExecutionState.performed);
            count = 0;
            foreach (var row in resultTable.Result.TableData)
            {
                CheckRow(row, row2.Result);
                count++;
            }
            Assert.AreEqual(count, 10);


        }

        [TestMethod]
        public void UpdateRowsTest()
        {
            const string testPath = "StorageTestUpdateRowsTest";
            if (Directory.Exists(testPath))
            {
                Directory.Delete(testPath, true);
            }
            dataStorage = new DataStorageInFiles(testPath, 805);

            var tableName = new List<string>() { "Table1" };
            var columns = new Dictionary<string, Column> {
                { "Name",new Column(new List<string>(){"Name"}, DataType.CHAR, 25, new List<string>(), NullSpecOpt.Null) },
                { "Soname",new Column(new List<string>(){"Soname"}, DataType.CHAR, 30, new List<string>(), NullSpecOpt.Null) },
                { "Age",new Column(new List<string>(){"Age"}, DataType.INT, 0, new List<string>(), NullSpecOpt.Null) },
                { "Rating",new Column(new List<string>(){"Rating" }, DataType.DOUBLE, 0, new List<string>(), NullSpecOpt.Null) },
                };
            var table = new Table(new TableMetaInf(tableName) { ColumnPool = columns });
            var result = dataStorage.AddTable(table);
            Assert.AreEqual(result.State, OperationExecutionState.performed);

            var count = 0;
            var row2 = table.CreateRowFormStr(new string[] { "Ivan", "IvanovIvanovIvanov", "23", "44.345" });
            Assert.AreEqual(row2.State, OperationExecutionState.performed);
            for (var i = 0; i < 10; ++i)
            {
                Assert.AreEqual(dataStorage.InsertRow(tableName, row2.Result).State, OperationExecutionState.performed);
            }

            var resultTable = dataStorage.LoadTable(table.TableMetaInf.Name);
            Assert.AreEqual(resultTable.State, OperationExecutionState.performed);
            count = 0;
            foreach (var row in resultTable.Result.TableData)
            {
                CheckRow(row, row2.Result);
                count++;
            }
            Assert.AreEqual(count, 10);

            var rowNotChange = table.CreateRowFormStr(new string[] { "Ivan", "IvanovIvanovIvanov", "100500", "44.345" });
            var rowChange = table.CreateRowFormStr(new string[] { "Gvanchik", "IvanovIvanovIvanov", "67", "44.345" });

            Assert.AreEqual(dataStorage.InsertRow(tableName, rowNotChange.Result).State, OperationExecutionState.performed);
            dataStorage.UpdateAllRow(table.TableMetaInf.Name, rowChange.Result, (Field[] f) => ((FieldChar)(f[0])).Value == ((FieldChar)(row2.Result[0])).Value);

            resultTable = dataStorage.LoadTable(table.TableMetaInf.Name);
            Assert.AreEqual(resultTable.State, OperationExecutionState.performed);
            count = 0;
            foreach (var row in resultTable.Result.TableData)
            {
                if (((FieldChar)(row[0])).Value == ((FieldChar)(rowNotChange.Result[0])).Value)
                {
                    CheckRow(row, rowNotChange.Result);
                }
                else
                {
                    CheckRow(row, rowChange.Result);
                }
                count++;
            }
            Assert.AreEqual(count, 11);

        }

        [TestMethod]
        public void DeleteRowsTest()
        {
            const string testPath = "StorageTestDeleteRowsTest";
            if (Directory.Exists(testPath))
            {
                Directory.Delete(testPath, true);
            }
            dataStorage = new DataStorageInFiles(testPath, 805);

            var tableName = new List<string>() { "Table1" };
            var columns = new Dictionary<string, Column> {
                { "Name",new Column(new List<string>(){"Name"}, DataType.CHAR, 25, new List<string>(), NullSpecOpt.Null) },
                { "Soname",new Column(new List<string>(){"Soname"}, DataType.CHAR, 30, new List<string>(), NullSpecOpt.Null) },
                { "Age",new Column(new List<string>(){"Age"}, DataType.INT, 0, new List<string>(), NullSpecOpt.Null) },
                { "Rating",new Column(new List<string>(){"Rating" }, DataType.DOUBLE, 0, new List<string>(), NullSpecOpt.Null) },
                };
            var table = new Table(new TableMetaInf(tableName) { ColumnPool = columns });
            var result = dataStorage.AddTable(table);
            Assert.AreEqual(result.State, OperationExecutionState.performed);

            var row1 = table.CreateDefaultRow();
            Assert.AreEqual(row1.State, OperationExecutionState.performed);
            //for (int i = 0; i < 10; ++i)
            //{
            //    Assert.AreEqual(row1.State, OperationExecutionState.performed);
            //    dataStorage.InsertRow(tableName, row1.Result);
            //}
            var row2 = table.CreateRowFormStr(new string[] { "Ivan", "IvanovIvanovIvanov", "23", "44.345" });
            Assert.AreEqual(row2.State, OperationExecutionState.performed);
            for (var i = 0; i < 10; ++i)
            {
                Assert.AreEqual(dataStorage.InsertRow(tableName, row2.Result).State, OperationExecutionState.performed);
            }
            dataStorage.InsertRow(tableName, row1.Result);
            dataStorage.RemoveAllRow(table.TableMetaInf.Name,(Field[] f)=> ((FieldChar)(f[0])).Value == ((FieldChar)(row2.Result[0])).Value );
            dataStorage.InsertRow(tableName, row1.Result);

            var resultTable = dataStorage.LoadTable(table.TableMetaInf.Name);
            Assert.AreEqual(resultTable.State, OperationExecutionState.performed);
            var count = 0;
            foreach (var row in resultTable.Result.TableData)
            {
                CheckRow(row, row1.Result);
                count++;
            }
            Assert.AreEqual(count, 2);
            for (var i = 0; i < 15; ++i)
            {

                Assert.AreEqual(dataStorage.InsertRow(tableName, row1.Result).State, OperationExecutionState.performed);
            }
            resultTable = dataStorage.LoadTable(table.TableMetaInf.Name);
            Assert.AreEqual(resultTable.State, OperationExecutionState.performed);
            count = 0;
            foreach (var row in resultTable.Result.TableData)
            {
                CheckRow(row, row1.Result);
                count++;
            }
            Assert.AreEqual(count, 17);

            dataStorage.RemoveAllRow(table.TableMetaInf.Name, (Field[] f) => ((FieldChar)(f[0])).Value == ((FieldChar)(row1.Result[0])).Value);
            count = 0;
            resultTable = dataStorage.LoadTable(table.TableMetaInf.Name);
            Assert.AreEqual(resultTable.State, OperationExecutionState.performed);
            count = 0;
            foreach (var row in resultTable.Result.TableData)
            {
                CheckRow(row, row1.Result);
                count++;
            }
            Assert.AreEqual(count, 0);
        }

            void CheckRow(Field[] a,Field[] b)
        {
            Assert.AreEqual(a.Length, b.Length);
            for (var i = 0; i < b.Length; i++)
            {
                switch (b[i].Type)
                {
                    case DataType.CHAR: Assert.AreEqual(((FieldChar)(a[i])).Value, ((FieldChar)(b[i])).Value); break;
                    case DataType.INT: Assert.AreEqual(((FieldInt)(a[i])).Value, ((FieldInt)(b[i])).Value); break;
                    case DataType.DOUBLE: Assert.AreEqual(((FieldDouble)(a[i])).Value, ((FieldDouble)(b[i])).Value); break;
                }

            }
        }

    }
}

