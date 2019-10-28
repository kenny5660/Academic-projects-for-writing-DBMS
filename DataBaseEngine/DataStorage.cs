﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using DataBaseErrors;

using DataBaseTable;

using Newtonsoft.Json;

using ZeroFormatter;

using DataBaseEngine;

namespace StorageEngine
{

    [ZeroFormattable]
    public class DataBlockNode
    {
        [Index(0)] public virtual int CountRealRecords { get; set; } = 0;
        [Index(1)] public virtual int CountNotDeletedRecords { get; set; } = 0;
        [Index(2)] public virtual int NextBlock { get; set; } = 0;
        [Index(3)] public virtual byte[] Data { get; set; } = null;

        public DataBlockNode()
        {

        }
        public DataBlockNode(DataBlockNode from)
        {
            CountRealRecords = from.CountRealRecords;
            CountNotDeletedRecords = from.CountNotDeletedRecords;
            NextBlock = from.NextBlock;
            Data = from.Data;
        }
        public DataBlockNode(int nextBlock, int dataSize)
        {
            NextBlock = nextBlock;
            Data = Enumerable.Repeat((byte)0x33, dataSize).ToArray();
            Data[0] = 77;
            CountRealRecords = 0;
            CountNotDeletedRecords = 0;
        }
        public bool InsertRecord(RowRecord record, int recordSize)
        {
            if (CountRealRecords * recordSize + recordSize > Data.Length)
            {
                return false;
            }

            using var memStream = new MemoryStream(Data);
            memStream.Seek(CountRealRecords * recordSize, SeekOrigin.Begin);
            ZeroFormatterSerializer.Serialize(memStream, record);
            CountRealRecords++;
            CountNotDeletedRecords++;
            return true;
        }
        public RecordsInDataBlockNodeEnumarator GetRowRecrodsEnumerator(int recordSize) => new RecordsInDataBlockNodeEnumarator(this, recordSize);
    }


    [ZeroFormattable]
    public class RowRecord
    {
        [Index(0)] public virtual bool IsDeleted { get; set; }
        [Index(1)] public virtual Field[] Fields { get; set; }
        public RowRecord()
        {

        }
        public RowRecord(Field[] fields)
        {
            Fields = fields;
            IsDeleted = false;
        }
    }

    public class RecordsInDataBlockNodeEnumarator : IEnumerator<RowRecord>
    {
        public RowRecord Current { get; private set; }

        object IEnumerator.Current => throw new NotImplementedException();

        private DataBlockNode dataBlock;
        private int recordSize;
        private int curPos;
        public RecordsInDataBlockNodeEnumarator(DataBlockNode dataBlock_, int recordSize_)
        {
            dataBlock = dataBlock_;
            recordSize = recordSize_;
            curPos = -1;
            MoveNext();
        }
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public bool MoveNext()
        {
            curPos++;
            if (curPos < dataBlock.CountRealRecords)
            {
                using var memStream = new MemoryStream(dataBlock.Data);
                memStream.Seek(curPos * recordSize, SeekOrigin.Begin);
                var recordBytes = new byte[recordSize];
                memStream.Read(recordBytes, 0, recordBytes.Length);
                Current = ZeroFormatterSerializer.Deserialize<RowRecord>(recordBytes);
                return Current.IsDeleted ? MoveNext() : true;
            }
            else
            {
                return false;
            }
        }

        public void Reset()
        {
            curPos = -1;
            MoveNext();
        }
    }

     class DataStorageRowsInFiles : IEnumerable<Field[]>
    {
        private TableFileManager tManager;
        public DataStorageRowsInFiles(TableFileManager tManager_)
        {
            tManager = tManager_;
        }
        public IEnumerator<Field[]> GetEnumerator()
        {
            return new DataStorageRowsInFilesEnumerator(tManager);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }

    class DataStorageRowsInFilesEnumerator : IEnumerator<Field[]>
    {
        public Field[] Current { get; private set; }
        object IEnumerator.Current => throw new NotImplementedException();

        private TableFileManager tManager;
        private IEnumerator<DataBlockNode> blocks;
        private IEnumerator<RowRecord> curRowRecordsEnumarator;

        public DataStorageRowsInFilesEnumerator(TableFileManager tManager_)
        {
            tManager = tManager_;
            Reset();

        }

        public void Dispose()
        {
            tManager.Dispose();
        }


        public bool MoveNext()
        {
            if (curRowRecordsEnumarator.MoveNext())
            {
                Current = curRowRecordsEnumarator.Current.Fields;
                return true;
            }
            else
            {
                if (blocks.MoveNext())
                {
                    curRowRecordsEnumarator = blocks.Current.GetRowRecrodsEnumerator(tManager.RowRecordSize);
                    Current = curRowRecordsEnumarator.Current.Fields;
                    return true;
                }
                else
                {
                    return false;
                }

            }
        }

        public void Reset()
        {
            blocks = tManager.GetBlockEnumarator();
            curRowRecordsEnumarator = blocks.Current.GetRowRecrodsEnumerator(tManager.RowRecordSize);
            Current = curRowRecordsEnumarator.Current.Fields;
        }
    }
    [ZeroFormattable]
    public class MetaInfDataStorage
    {
        [Index(0)] public virtual int TableMetaInfSize { get; set; } = 0;
        [Index(1)] public virtual int RowRecordSize { get; set; } = 0;
        [Index(2)] public virtual int DataBlockSize { get; set; } = 0;
        [Index(3)] public virtual int HeadFreeBlockList { get; set; } = 0;
        [Index(4)] public virtual int HeadDataBlockList { get; set; } = 0;
    }


    public interface IDataStorage
    {
        OperationResult<Table> LoadTable(string tableName);
        OperationResult<bool> ContainsTable(string tableName);
        OperationResult<string> AddTable(Table table);
        OperationResult<string> RemoveTable(string tableName);

        OperationResult<string> UpdateAllRow(string tableName, Field[] newRow, Predicate<Field[]> match);
        OperationResult<string> InsertRow(string tableName, Field[] fields);
        OperationResult<string> RemoveAllRow(string tableName, Predicate<Field[]> match);
    }

    public class DataStorageInFiles : IDataStorage
    {
        public string PathToDataBase { get; set; }
        private const string _fileMarkTableMetaInf = "DATA_BASE_TABLE_METAINF_FILE";
        private const string _fileExtension = ".tdb";
        private int blockSize = 4096;
        public DataStorageInFiles(string path, int blockSize_)
        {
            blockSize = blockSize_;
            PathToDataBase = path;
            if (!Directory.Exists(path))
            {
                CreateDataStorageFolder(path);
            }
        }

        public OperationResult<Table> LoadTable(string tableName)
        {
            if (!File.Exists(GetTableFileName(tableName)))
            {
                return new OperationResult<Table>(OperationExecutionState.failed, null, new TableNotExistExeption(tableName));
            }
            Table table;
            var tManager = new TableFileManager(new FileStream(GetTableFileName(tableName), FileMode.Open));
            table = tManager.LoadTable();
            return new OperationResult<Table>(OperationExecutionState.performed, table);
        }

        public OperationResult<bool> ContainsTable(string tableName)
        {
            if (File.Exists(GetTableFileName(tableName))) {
                return new OperationResult<bool>(OperationExecutionState.performed, true);
            }
            else
            {
                return new OperationResult<bool>(OperationExecutionState.failed, false);
            }
        }

        public OperationResult<string> AddTable(Table table)
        {
            if (File.Exists(GetTableFileName(table.TableMetaInf.Name)))
            {
                return new OperationResult<string>(OperationExecutionState.failed, null, new TableAlreadyExistExeption(table.TableMetaInf.Name));
            }
            using var tManager = new TableFileManager(new FileStream(GetTableFileName(table.TableMetaInf.Name), FileMode.Create), table, blockSize);
            return new OperationResult<string>(OperationExecutionState.performed, "");
        }

        public OperationResult<string> RemoveTable(string tableName)
        {
            if (!File.Exists(GetTableFileName(tableName)))
            {
                return new OperationResult<string>(OperationExecutionState.failed, null, new TableNotExistExeption(tableName));
            }
            File.Delete(GetTableFileName(tableName));
            return new OperationResult<string>(OperationExecutionState.performed, "");
        }

        public OperationResult<string> UpdateAllRow(string tableName, Field[] newRow, Predicate<Field[]> match)
        {
            throw new NotImplementedException();
        }

        public OperationResult<string> InsertRow(string tableName, Field[] fields)
        {
            if (!File.Exists(GetTableFileName(tableName)))
            {
                return new OperationResult<string>(OperationExecutionState.failed, null, new TableNotExistExeption(tableName));
            }
            using (var manager = new TableFileManager(new FileStream(GetTableFileName(tableName), FileMode.Open)))
            {
                var rowRecord = new RowRecord(fields);
                manager.InsertRecord(rowRecord);
            }
            return new OperationResult<string>(OperationExecutionState.performed, "");

        }

        public OperationResult<string> RemoveAllRow(string tableName, Predicate<Field[]> match)
        {
            throw new NotImplementedException();
        }

        private void CreateDataStorageFolder(string path)
        {
            Directory.CreateDirectory(path);
        }

        private string GetTableFileName(string tableName) => PathToDataBase + "/" + tableName + _fileExtension;

    }


    class TableFileManagerDataBlockNodeEnumerator : IEnumerator<DataBlockNode>
    {
        object IEnumerator.Current => throw new NotImplementedException();

        public DataBlockNode Current { get; private set; }

        private TableFileManager tManager;

        public TableFileManagerDataBlockNodeEnumerator(TableFileManager tManager_)
        {
            tManager = tManager_;
            Current = tManager.LoadHeadDataBlock();
        }

        public void Dispose()
        {

        }


        public bool MoveNext()
        {
            if (Current.NextBlock == 0)
            {
                Current = tManager.LoadDataBlock(Current.NextBlock);
                return true;
            }
            else
            {
                return false;
            }
        }

        public void Reset()
        {
            Current = tManager.LoadHeadDataBlock();
        }
    }

    class TableFileManager: IDisposable
    {
        private FileStream fs;
        public int RowRecordSize
        {
            get
            {
                return metaInfDataStorage.RowRecordSize;
            }
        }
        private MetaInfDataStorage metaInfDataStorage;
        public TableFileManager(FileStream fs_)
        {
            fs = fs_;
            metaInfDataStorage = LoadMetaInfStorage();
        }
        public TableFileManager(FileStream fs_, Table table, int blockSize)
        {
            fs = fs_;
            using var memStream = new MemoryStream();
            ZeroFormatterSerializer.Serialize(memStream, table.TableMetaInf);
            var metaInfStorage = new MetaInfDataStorage { TableMetaInfSize = (int)memStream.Length, RowRecordSize = CalculateRowRecordSize(table), DataBlockSize = blockSize, HeadDataBlockList = 0, HeadFreeBlockList = 0 };
            memStream.WriteTo(fs);
            SaveMetaInfStorage(metaInfStorage);
            metaInfDataStorage = LoadMetaInfStorage();
        }
        private int CalculateDataBlockNodeSize()
        {
            using var memStream = new MemoryStream();
            var dataBlock = new DataBlockNode(0, 1);
            ZeroFormatterSerializer.Serialize(memStream, dataBlock);
            return metaInfDataStorage.DataBlockSize - (int)memStream.Length+1;
        }

        private int CalculateMetaInfDataStorageSize()
        {
            using var memStream = new MemoryStream();
            var dataBlock = new MetaInfDataStorage
            {
                DataBlockSize = int.MaxValue,
                RowRecordSize = int.MaxValue,
                HeadDataBlockList = int.MaxValue,
                HeadFreeBlockList = int.MaxValue,
                TableMetaInfSize = int.MaxValue
            };
            ZeroFormatterSerializer.Serialize(memStream, dataBlock);
            return (int)memStream.Length;
        }

        private int CalculateRowRecordSize(Table table)
        {
            using var memStream = new MemoryStream();
            var rowRecord = new RowRecord(table.CreateDefaultRow().Result);
            ZeroFormatterSerializer.Serialize(memStream, rowRecord);
            return (int)memStream.Length;
        }
        public void InsertRecord(RowRecord rowRecord)
        {
            if (metaInfDataStorage.HeadDataBlockList == 0)
            {
                CreateAndAddDataBlock();
            }
            var dataBlock = LoadDataBlock(metaInfDataStorage.HeadDataBlockList);
            if (!dataBlock.InsertRecord(rowRecord, metaInfDataStorage.RowRecordSize))
            {
                MoveNewBlockToHead();
                dataBlock = LoadDataBlock(metaInfDataStorage.HeadDataBlockList);
                dataBlock.InsertRecord(rowRecord, metaInfDataStorage.RowRecordSize);
            }
            SaveDataBlock(dataBlock, metaInfDataStorage.HeadDataBlockList);
        }
        private void SaveMetaInfStorage(MetaInfDataStorage meta)
        {
            fs.Seek(0, SeekOrigin.End);
            ZeroFormatterSerializer.Serialize(fs, meta);
        }

        private MetaInfDataStorage LoadMetaInfStorage()
        {
            fs.Seek(-CalculateMetaInfDataStorageSize(), SeekOrigin.End);
            return ZeroFormatterSerializer.Deserialize<MetaInfDataStorage>(fs);
        }
        public void MoveNewBlockToHead()
        {
            if (metaInfDataStorage.HeadFreeBlockList == 0)
            {
                CreateAndAddDataBlock();
            }
            else
            {
                var deletedBlock = LoadDataBlock(metaInfDataStorage.HeadFreeBlockList);
                var delBlockOffset = metaInfDataStorage.HeadFreeBlockList;
                metaInfDataStorage.HeadFreeBlockList = deletedBlock.NextBlock;
                deletedBlock.NextBlock = metaInfDataStorage.HeadDataBlockList;
                metaInfDataStorage.HeadDataBlockList = delBlockOffset;
                SaveDataBlock(deletedBlock, metaInfDataStorage.HeadDataBlockList);
                SaveMetaInfStorage(metaInfDataStorage);
            }
        }
        public void SaveDataBlock(DataBlockNode block, int offset)
        {
            fs.Seek(offset, SeekOrigin.Begin);
            //using var memStream = new MemoryStream();
            block = new DataBlockNode(block);
            ZeroFormatterSerializer.Serialize(fs, block);
            //memStream.CopyTo(fs);
            fs.Flush(true);
        }
        public void CreateAndAddDataBlock()
        {
            var metaInf = metaInfDataStorage;
            DataBlockNode newBlock;
            if (metaInf.HeadDataBlockList != 0)
            {
                var prevBlock = LoadDataBlock(metaInf.HeadDataBlockList);
                newBlock = new DataBlockNode(metaInf.HeadDataBlockList, CalculateDataBlockNodeSize());
            }
            else
            {
                newBlock = new DataBlockNode(0, CalculateDataBlockNodeSize());
            }
            metaInf.HeadDataBlockList = (int)fs.Seek(-CalculateMetaInfDataStorageSize(), SeekOrigin.End);
            ZeroFormatterSerializer.Serialize(fs, newBlock);
            SaveMetaInfStorage(metaInf);
        }

        public DataBlockNode LoadDataBlock(int offset)
        {
            fs.Seek(offset, SeekOrigin.Begin);
            var buffer = new byte[metaInfDataStorage.DataBlockSize];
            fs.Read(buffer, 0, metaInfDataStorage.DataBlockSize);
            using var memStream = new MemoryStream();
            memStream.Write(buffer, 0, buffer.Length);
            memStream.Seek(0, SeekOrigin.Begin);
            return ZeroFormatterSerializer.Deserialize<DataBlockNode>(memStream);
        }
        public DataBlockNode LoadHeadDataBlock()
        {
           return LoadDataBlock(metaInfDataStorage.HeadDataBlockList);
        }
        public TableFileManagerDataBlockNodeEnumerator GetBlockEnumarator()
        {
            return new TableFileManagerDataBlockNodeEnumerator(this);
        }
        public Table LoadTable()
        {
            var table = new Table();
            fs.Seek(0, SeekOrigin.Begin);
            var rawTable = new byte[metaInfDataStorage.TableMetaInfSize];
            fs.Read(rawTable, 0, rawTable.Length);
            table.TableMetaInf = ZeroFormatterSerializer.Deserialize<TableMetaInf>(rawTable);
            table.TableData = new DataStorageRowsInFiles(this);
            return table;
        }

        public void Dispose()
        {
            fs.Dispose();
        }
    }
}
