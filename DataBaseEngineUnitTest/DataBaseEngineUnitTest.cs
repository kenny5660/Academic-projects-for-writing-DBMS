
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace DataBaseEngineUnitTest
{
    [TestClass]
    public class DataBaseUnitTests
    {
        //DataBaseEngineMain dataBase;
        //const string testConfigPath = "TestConfig.json";
        //[TestInitialize]
        //public void TestInitialize() => dataBase = new DataBaseEngineMain(testConfigPath);

        //[TestCleanup]
        //public void TestCleanup()
        //{
        //    if (File.Exists(dataBase.EngineConfig.Path))
        //    {
        //        File.Delete(dataBase.EngineConfig.Path);
        //    }
        //}

        //[TestMethod]
        //public void AddTableOnlyNameTest()
        //{
        //    var tableName = "Table1";
        //    var result = dataBase.CreateTable(tableName);
        //    Assert.AreEqual(result.State, OperationExecutionState.performed);
        //    Assert.AreEqual(dataBase.TablePool.ContainsKey(tableName), true);
        //    result = dataBase.CreateTable(tableName);
        //    Assert.AreEqual(result.State, OperationExecutionState.failed);
        //}


        //[TestMethod]
        //public void SaveLoadTablePoolTest()
        //{
        //    var tableName = "Table1";
        //    var tableName2 = "Table2";
        //    var column = new Column("age", ColumnDataType.CHAR, 0, new List<string>(), NullSpecOpt.Null);

        //    var result = dataBase.CreateTable(tableName);
        //    dataBase.AddColumnToTable(tableName, column);
        //    Assert.AreEqual(result.State, OperationExecutionState.performed);
        //    var result2 = dataBase.Commit();
        //    dataBase = new DataBaseEngineMain(testConfigPath);
        //    Assert.AreEqual(dataBase.TablePool.ContainsKey(tableName), true);
        //    result = dataBase.CreateTable(tableName2);
        //    dataBase.AddColumnToTable(tableName2, column);
        //    Assert.AreEqual(result.State, OperationExecutionState.performed);
        //    result2 = dataBase.Commit();
        //    Assert.AreEqual(result2.State, OperationExecutionState.performed);
        //    dataBase = new DataBaseEngineMain(testConfigPath);
        //    Assert.AreEqual(dataBase.TablePool.ContainsKey(tableName), true);
        //    Assert.AreEqual(dataBase.TablePool.ContainsKey(tableName2), true);
        //    Assert.AreEqual(dataBase.TablePool[tableName].TableMetaInf.ColumnPool.ContainsKey(column.Name), true);
        //}
        //[TestMethod]
        //public void SaveLoadTableData()
        //{
        //    var tableName = "Table1";

        //    var column = new Column("age", ColumnDataType.CHAR, 0, new List<string>(), NullSpecOpt.Null);

        //    var result = dataBase.CreateTable(tableName);
        //    dataBase.AddColumnToTable(tableName, column);
        //    Assert.AreEqual(result.State, OperationExecutionState.performed);
        //    dataBase.TablePool[tableName].TableData = new TableData
        //    {
        //        Rows = new List<Dictionary<string, Field>> {
        //        { new Dictionary<string, Field> {
        //            {"id", new FieldInt { Value = 30 }},
        //            {"name", new FieldChar("Ivanov",30)},
        //        }
        //        },
        //        { new Dictionary<string, Field> {
        //            {"id", new FieldInt { Value = 30 }},
        //            {"name", new FieldChar("Ivanov",30)},
        //         }
        //        }
        //    }
        //    };

        //    var result2 = dataBase.Commit();
        //    Assert.AreEqual(result2.State, OperationExecutionState.performed);

        //    var resultload = dataBase.DataStorage.LoadTableData(dataBase.TablePool[tableName]);
        //    Assert.AreEqual(resultload.State, OperationExecutionState.performed);
        //    Assert.AreEqual(dataBase.TablePool[tableName].TableData.Rows.Count, 2);
        //    foreach (var L in dataBase.TablePool[tableName].TableData.Rows)
        //    {
        //        Assert.AreEqual(((FieldInt)L["id"]).Value, 30);
        //        Assert.AreEqual(((FieldChar)L["name"]).Value, "Ivanov");
        //    }
        //}

        //[TestMethod]
        //public void CommitTest()
        //{
        //    var tableName = "Table1";
        //    var tableName2 = "Table2";
        //    var column = new Column("age", ColumnDataType.CHAR, 0, new List<string>(), NullSpecOpt.Null);

        //    var result = dataBase.CreateTable(tableName);
        //    dataBase.AddColumnToTable(tableName, column);
        //    Assert.AreEqual(result.State, OperationExecutionState.performed);
        //    result = dataBase.CreateTable(tableName2);
        //    dataBase.AddColumnToTable(tableName2, column);
        //    Assert.AreEqual(result.State, OperationExecutionState.performed);
        //    var result2 = dataBase.Commit();
        //    Assert.AreEqual(result2.State, OperationExecutionState.performed);
        //    dataBase = new DataBaseEngineMain(testConfigPath);
        //    Assert.AreEqual(result2.State, OperationExecutionState.performed);
        //    Assert.AreEqual(dataBase.TablePool.ContainsKey(tableName), true);
        //    Assert.AreEqual(dataBase.TablePool.ContainsKey(tableName2), true);
        //    Assert.AreEqual(dataBase.TablePool[tableName].TableMetaInf.ColumnPool.ContainsKey(column.Name), true);
        //}

        //[TestMethod]
        //public void AddDeleteColumnTest()
        //{
        //    var tableName = "Table1";
        //    var columnName = "age";
        //    var columnType = ColumnDataType.DOUBLE;
        //    var column = new Column(columnName, columnType, 0, new List<string>(), NullSpecOpt.Null);
        //    var result = dataBase.CreateTable(tableName);
        //    Assert.AreEqual(result.State, OperationExecutionState.performed);
        //    Assert.AreEqual(dataBase.TablePool.ContainsKey(tableName), true);
        //    result = dataBase.AddColumnToTable(tableName, column);
        //    Assert.AreEqual(result.State, OperationExecutionState.performed);
        //    Assert.AreEqual(dataBase.TablePool[tableName].TableMetaInf.ColumnPool.ContainsKey(columnName), true);
        //    result = dataBase.DeleteColumnFromTable(tableName, column.Name);
        //    Assert.AreEqual(result.State, OperationExecutionState.performed);
        //    Assert.AreEqual(dataBase.TablePool[tableName].TableMetaInf.ColumnPool.ContainsKey(columnName), false);
        //}
    }
}
