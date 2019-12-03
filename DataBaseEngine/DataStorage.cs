﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using DataBaseType;
using ZeroFormatter;

namespace StorageEngine
{
    [ZeroFormattable]
    public class DataBlockNode
    {
        [Index(0)]
        public virtual int CountRealRecords { get; set; } = 0;

        [Index(1)]
        public virtual int CountNotDeletedRecords { get; set; } = 0;

        [Index(2)]
        public virtual int NextBlock { get; set; } = 0;

        [Index(3)]
        public virtual int PrevBlock { get; set; } = 0;

        [Index(4)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "<Ожидание>")]
        public virtual byte[] Data { get; set; } = null;

        public DataBlockNode()
        {

        }

        public DataBlockNode(DataBlockNode from)
        {
            if (from is null)
            {
                throw new ArgumentNullException(nameof(from));
            }

            CountRealRecords = from.CountRealRecords;
            CountNotDeletedRecords = from.CountNotDeletedRecords;
            NextBlock = from.NextBlock;
            PrevBlock = from.PrevBlock;
            Data = from.Data;
        }

        public DataBlockNode(int prevBlock, int nextBlock, int dataSize)
        {
            PrevBlock = prevBlock;
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

            SaveRecord(record, CountRealRecords, recordSize);
            CountRealRecords++;
            CountNotDeletedRecords++;
            return true;
        }
        public RowRecord LoadRowRecord(int pos, int recordSize)
        {
            if (pos < CountRealRecords)
            {
                using var memStream = new MemoryStream(Data);
                memStream.Seek(pos * recordSize, SeekOrigin.Begin);
                var recordBytes = new byte[recordSize];
                memStream.Read(recordBytes, 0, recordBytes.Length);
                return ZeroFormatterSerializer.Deserialize<RowRecord>(recordBytes);
            }
            else
            {
                return null;
            }
        }
        public void SaveRecord(RowRecord record, int pos, int recordSize)
        {
            using var memStream = new MemoryStream(Data);
            memStream.Seek(pos * recordSize, SeekOrigin.Begin);
            var buffer = new byte[recordSize];
            ZeroFormatterSerializer.Serialize(ref buffer, 0, record);
            memStream.Write(buffer, 0, buffer.Length);
        }
        public bool DeleteRecord(int pos, int recordSize)
        {
            var rowRecord = LoadRowRecord(pos, recordSize);
            rowRecord.IsDeleted = true;
            SaveRecord(rowRecord, pos, recordSize);
            CountNotDeletedRecords--;
            return CountNotDeletedRecords == 0;
        }
        public bool UpdateRecord(RowRecord newRecord, int pos, int recordSize)
        {
            SaveRecord(newRecord, pos, recordSize);
            return true;
        }
        public RecordsInDataBlockNodeEnumarator GetRowRecrodsEnumerator(int recordSize) => new RecordsInDataBlockNodeEnumarator(this, recordSize);
    }


    [ZeroFormattable]
    public class RowRecord
    {
        [Index(0)]
        public virtual bool IsDeleted { get; set; }

        [Index(1)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "<Ожидание>")]
        public virtual Field[] Fields { get; set; }

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

        private readonly DataBlockNode _dataBlock;
        private readonly int _recordSize;
        private int _curPos;
        private bool _disposed = false;

        public RecordsInDataBlockNodeEnumarator(DataBlockNode dataBlock, int recordSize)
        {
            _dataBlock = dataBlock;
            _recordSize = recordSize;
            Reset();
        }

        // Public implementation of Dispose pattern callable by consumers.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                // Free any other managed objects here.
            }

            _disposed = true;
        }

        public bool MoveNext()
        {
            _curPos++;
            Current = _dataBlock.LoadRowRecord(_curPos, _recordSize);
            return Current != null ? Current.IsDeleted ? MoveNext() : true : false;
        }

        public bool DeleteCurRow() => _dataBlock.DeleteRecord(_curPos, _recordSize);

        public bool UpdateCurRow(RowRecord rowRecord) => _dataBlock.UpdateRecord(rowRecord, _curPos, _recordSize);

        public void Reset() => _curPos = -1;
    }

    internal class DataStorageRowsInFiles : IEnumerable<Field[]>
    {
        private readonly TableFileManager _tManager;

        public DataStorageRowsInFiles(TableFileManager tManager_) => _tManager = tManager_;
        public IEnumerator<Field[]> GetEnumerator() => new DataStorageRowsInFilesEnumerator(_tManager);

        IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();

    }

    internal class DataStorageRowsInFilesEnumerator : IEnumerator<Field[]>
    {
        public Field[] Current { get; private set; }
        object IEnumerator.Current => throw new NotImplementedException();

        private readonly TableFileManager _tManager;
        private TableFileManagerDataBlockNodeEnumerator _blocks;
        private RecordsInDataBlockNodeEnumarator _curRowRecordsEnumarator;
        private bool _disposed = false;

        public DataStorageRowsInFilesEnumerator(TableFileManager tManager)
        {
            _tManager = tManager;
            Reset();

        }

        // Public implementation of Dispose pattern callable by consumers.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _tManager.Dispose();
                _blocks.Dispose();
                _curRowRecordsEnumarator.Dispose();

            }

            _disposed = true;
        }

        public bool UpdateCurrentRow(Field[] newRow)
        {
            _curRowRecordsEnumarator.UpdateCurRow(new RowRecord(newRow));
            _tManager.SaveDataBlock(_blocks.Current, _blocks.CurrentOffset);
            return MoveNext();
        }

        public bool DeleteCurrentRow()
        {
            var res = _curRowRecordsEnumarator.DeleteCurRow();
            var prevBlock = _blocks.Current;

            if (res)
            {
                _tManager.DeleteBlock(prevBlock);
            }
            else
            {
                _tManager.SaveDataBlock(_blocks.Current, _blocks.CurrentOffset);
            }

            return MoveNext();
        }

        public bool MoveNext()
        {
            if (_curRowRecordsEnumarator == null)
            {
                var res = _blocks.MoveNext();
                if (!res)
                {
                    return res;
                }
                _curRowRecordsEnumarator = _blocks.Current.GetRowRecrodsEnumerator(_tManager.RowRecordSize);
            }
            if (_curRowRecordsEnumarator.MoveNext())
            {
                Current = _curRowRecordsEnumarator.Current.Fields;
                return true;
            }
            else
            {
                if (_blocks.MoveNext())
                {
                    _curRowRecordsEnumarator = _blocks.Current.GetRowRecrodsEnumerator(_tManager.RowRecordSize);
                    _curRowRecordsEnumarator.MoveNext();
                    Current = _curRowRecordsEnumarator.Current.Fields;
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
            _blocks = _tManager.GetBlockEnumarator();
            _curRowRecordsEnumarator = null;
            Current = null;
        }
    }
    [ZeroFormattable]
    public class MetaInfDataStorage
    {
        [Index(0)]
        public virtual int TableMetaInfSize { get; set; } = 0;

        [Index(1)]
        public virtual int RowRecordSize { get; set; } = 0;

        [Index(2)]
        public virtual int DataBlockSize { get; set; } = 0;

        [Index(3)]
        public virtual int HeadFreeBlockList { get; set; } = 0;

        [Index(4)]
        public virtual int HeadDataBlockList { get; set; } = 0;
    }


    public interface IDataStorage
    {
        OperationResult<Table> LoadTable(List<string> tableName);
        OperationResult<bool> ContainsTable(List<string> tableName);
        OperationResult<string> AddTable(Table table);
        OperationResult<string> RemoveTable(List<string> tableName);

        OperationResult<string> UpdateAllRow(List<string> tableName, Field[] newRow, Predicate<Field[]> match);
        OperationResult<string> InsertRow(List<string> tableName, Field[] fields);
        OperationResult<string> RemoveAllRow(List<string> tableName, Predicate<Field[]> match);
    }

    public class DataStorageInFiles : IDataStorage
    {
        public string PathToDataBase { get; set; }

        private const string _fileExtension = ".tdb";
        private readonly int _blockSize = 4096;
        public DataStorageInFiles(string path, int blockSize)
        {
            _blockSize = blockSize;
            PathToDataBase = path;
            if (!Directory.Exists(path))
            {
                CreateDataStorageFolder(path);
            }
        }

        public OperationResult<Table> LoadTable(List<string> tableName)
        {
            if (!File.Exists(GetTableFileName(tableName)))
            {
                return new OperationResult<Table>(OperationExecutionState.failed, null, new TableNotExistExeption(FullTableName(tableName)));
            }

            using var tManager = new TableFileManager(new FileStream(GetTableFileName(tableName), FileMode.Open));

            var table = tManager.LoadTable();

            return new OperationResult<Table>(OperationExecutionState.performed, table);
        }
        private string FullTableName(List<string> tableName)
        {
            var sb = new StringBuilder();
            foreach (var n in tableName)
            {
                sb.Append(n);
            }
            return sb.ToString();
        }
        public OperationResult<bool> ContainsTable(List<string> tableName)
        {
            if (File.Exists(GetTableFileName(tableName)))
            {
                return new OperationResult<bool>(OperationExecutionState.performed, true);
            }
            else
            {
                throw new ArgumentNullException(nameof(table));
            }

        public OperationResult<string> AddTable(Table table)
        {
            if (File.Exists(GetTableFileName(table.TableMetaInf.Name)))
            {
                return new OperationResult<string>(OperationExecutionState.failed, null, new TableAlreadyExistExeption(FullTableName(table.TableMetaInf.Name)));
            }
            using var tManager = new TableFileManager(new FileStream(GetTableFileName(table.TableMetaInf.Name), FileMode.Create), table, blockSize);
            return new OperationResult<string>(OperationExecutionState.performed, "");
        }

        public OperationResult<string> RemoveTable(List<string> tableName)
        {
            if (!File.Exists(GetTableFileName(tableName)))
            {
                return new OperationResult<string>(OperationExecutionState.failed, null, new TableNotExistExeption(FullTableName(tableName)));
            }

            File.Delete(GetTableFileName(tableName));

            return new OperationResult<string>(OperationExecutionState.performed, "");
        }

        public OperationResult<string> UpdateAllRow(List<string> tableName, Field[] newRow, Predicate<Field[]> match)
        {
            if (!File.Exists(GetTableFileName(tableName)))
            {
                return new OperationResult<string>(OperationExecutionState.failed, null, new TableNotExistExeption(FullTableName(tableName)));
            }

            using (var manager = new TableFileManager(new FileStream(GetTableFileName(tableName), FileMode.Open)))
            {
                using var tableData = new DataStorageRowsInFilesEnumerator(manager);
                var isnLast = tableData.MoveNext();
                while (isnLast)
                {
                    isnLast = match(tableData.Current) ? tableData.UpdateCurrentRow(newRow) : tableData.MoveNext();
                }
            }

            return new OperationResult<string>(OperationExecutionState.performed, "");
        }

        public OperationResult<string> InsertRow(List<string> tableName, Field[] fields)
        {
            if (!File.Exists(GetTableFileName(tableName)))
            {
                return new OperationResult<string>(OperationExecutionState.failed, null, new TableNotExistExeption(FullTableName(tableName)));
            }

            using (var manager = new TableFileManager(new FileStream(GetTableFileName(tableName), FileMode.Open)))
            {
                var rowRecord = new RowRecord(fields);
                manager.InsertRecord(rowRecord);
            }

            return new OperationResult<string>(OperationExecutionState.performed, "");
        }

        public OperationResult<string> RemoveAllRow(List<string> tableName, Predicate<Field[]> match)
        {

            if (!File.Exists(GetTableFileName(tableName)))
            {
                return new OperationResult<string>(OperationExecutionState.failed, null, new TableNotExistExeption(FullTableName(tableName)));
            }

            using (var manager = new TableFileManager(new FileStream(GetTableFileName(tableName), FileMode.Open)))
            {
                using var tableData = new DataStorageRowsInFilesEnumerator(manager);
                var isnLast = tableData.MoveNext();
                while (isnLast)
                {
                    isnLast = match(tableData.Current) ? tableData.DeleteCurrentRow() : tableData.MoveNext();
                }
            }

            return new OperationResult<string>(OperationExecutionState.performed, "");
        }

        private void CreateDataStorageFolder(string path) => Directory.CreateDirectory(path);

        private string GetTableFileName(List<string> tableName) => PathToDataBase + "/" + FullTableName(tableName) + _fileExtension;

    }

    internal class TableFileManagerDataBlockNodeEnumerator : IEnumerator<DataBlockNode>
    {
        object IEnumerator.Current => throw new NotImplementedException();

        public DataBlockNode Current { get; private set; }
        public int CurrentOffset { get; set; }

        private readonly TableFileManager _tManager;
        private bool _disposed = false;

        public TableFileManagerDataBlockNodeEnumerator(TableFileManager tManager_)
        {
            _tManager = tManager_;
            Reset();
        }

        // Public implementation of Dispose pattern callable by consumers.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _tManager.Dispose();
                // Free any other managed objects here.
                //
            }

            _disposed = true;
        }

        public bool MoveNext()
        {
            if (Current == null)
            {
                CurrentOffset = _tManager.metaInfDataStorage.HeadDataBlockList;
                Current = _tManager.LoadHeadDataBlock();
                return Current != null;
            }

            if (Current.NextBlock != 0)
            {
                CurrentOffset = Current.NextBlock;
                Current = _tManager.LoadDataBlock(Current.NextBlock);
                return true;
            }
            else
            {
                return false;
            }
        }

        public void Reset() => CurrentOffset = _tManager.metaInfDataStorage.HeadDataBlockList;// Current = tManager.LoadHeadDataBlock();
    }

    internal class TableFileManager : IDisposable
    {
        private readonly FileStream _fileStream;
        public int RowRecordSize => metaInfDataStorage.RowRecordSize;
        public MetaInfDataStorage metaInfDataStorage;

        public TableFileManager(FileStream fileStream)
        {
            _fileStream = fileStream;
            metaInfDataStorage = LoadMetaInfStorage();
        }

        public TableFileManager(FileStream fs_, Table table, int blockSize)
        {
            _fileStream = fs_;
            using var memStream = new MemoryStream();
            ZeroFormatterSerializer.Serialize(memStream, table.TableMetaInf);
            var metaInfStorage = new MetaInfDataStorage { TableMetaInfSize = (int)memStream.Length, RowRecordSize = CalculateRowRecordSize(table), DataBlockSize = blockSize, HeadDataBlockList = 0, HeadFreeBlockList = 0 };
            memStream.WriteTo(_fileStream);
            CreateMetaInfInEnd(metaInfStorage);
            metaInfDataStorage = LoadMetaInfStorage();
        }

        private int CalculateDataBlockNodeSize()
        {
            using var memStream = new MemoryStream();
            var dataBlock = new DataBlockNode(0, 0, 1);
            ZeroFormatterSerializer.Serialize(memStream, dataBlock);
            return metaInfDataStorage.DataBlockSize - (int)memStream.Length + 1;
        }

        private static int GetCalculateMetaInfDataStorageSize()
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

            var dataBlock = LoadDataBlock(metaInfDataStorage.HeadDataBlockList);

            if (dataBlock == null || !dataBlock.InsertRecord(rowRecord, metaInfDataStorage.RowRecordSize))
            {
                MoveNewBlockToHead();
                dataBlock = LoadDataBlock(metaInfDataStorage.HeadDataBlockList);
                dataBlock.InsertRecord(rowRecord, metaInfDataStorage.RowRecordSize);
            }
            SaveDataBlock(dataBlock, metaInfDataStorage.HeadDataBlockList);
        }

        private void CreateMetaInfInEnd(MetaInfDataStorage meta)
        {
            _fileStream.Seek(0, SeekOrigin.End);
            ZeroFormatterSerializer.Serialize(_fileStream, meta);
            metaInfDataStorage = LoadMetaInfStorage();
        }

        private void SaveMetaInfStorage(MetaInfDataStorage meta)
        {
            _fileStream.Seek(-GetCalculateMetaInfDataStorageSize(), SeekOrigin.End);
            ZeroFormatterSerializer.Serialize(_fileStream, meta);
            metaInfDataStorage = LoadMetaInfStorage();
        }

        private MetaInfDataStorage LoadMetaInfStorage()
        {
            _fileStream.Seek(-GetCalculateMetaInfDataStorageSize(), SeekOrigin.End);
            return ZeroFormatterSerializer.Deserialize<MetaInfDataStorage>(_fileStream);
        }

        public void DeleteBlock(DataBlockNode block)
        {
            var nextBlock = LoadDataBlock(block.NextBlock);
            var prevBlock = LoadDataBlock(block.PrevBlock);
            var curBlockOff = prevBlock == null ? metaInfDataStorage.HeadDataBlockList : prevBlock.NextBlock;

            if (nextBlock != null)
            {
                nextBlock.PrevBlock = block.PrevBlock;
                SaveDataBlock(nextBlock, block.NextBlock);
            }

            if (prevBlock != null)
            {
                prevBlock.NextBlock = block.NextBlock;
                SaveDataBlock(prevBlock, block.PrevBlock);
            }
            else
            {
                metaInfDataStorage.HeadDataBlockList = block.NextBlock;
            }

            if (metaInfDataStorage.HeadFreeBlockList == 0)
            {

                metaInfDataStorage.HeadFreeBlockList = curBlockOff;
                block = new DataBlockNode(0, 0, CalculateDataBlockNodeSize());
            }
            else
            {
                var prevDelBlock = LoadDataBlock(metaInfDataStorage.HeadFreeBlockList);
                block = new DataBlockNode(0, metaInfDataStorage.HeadFreeBlockList, CalculateDataBlockNodeSize());
                metaInfDataStorage.HeadFreeBlockList = curBlockOff;
                prevDelBlock.PrevBlock = metaInfDataStorage.HeadFreeBlockList;
                SaveDataBlock(prevDelBlock, block.NextBlock);
            }

            SaveDataBlock(block, metaInfDataStorage.HeadFreeBlockList);
            SaveMetaInfStorage(metaInfDataStorage);
        }

        public void MoveNewBlockToHead()
        {
            if (metaInfDataStorage.HeadFreeBlockList == 0)
            {
                CreateAndAddDataBlock();
            }
            else
            {
                var oldBlock = LoadDataBlock(metaInfDataStorage.HeadDataBlockList);
                var oldBlockOffset = metaInfDataStorage.HeadDataBlockList;
                var deletedBlock = LoadDataBlock(metaInfDataStorage.HeadFreeBlockList);
                var deletedBlockNext = LoadDataBlock(deletedBlock.NextBlock);
                var delBlockOffset = metaInfDataStorage.HeadFreeBlockList;
                var delBlockNextOffset = deletedBlock.NextBlock;

                if (deletedBlockNext != null)
                {
                    deletedBlockNext.PrevBlock = 0;
                    SaveDataBlock(deletedBlockNext, delBlockNextOffset);
                }

                metaInfDataStorage.HeadFreeBlockList = delBlockNextOffset;
                deletedBlock.NextBlock = metaInfDataStorage.HeadDataBlockList;
                oldBlock.PrevBlock = delBlockOffset;
                metaInfDataStorage.HeadDataBlockList = delBlockOffset;

                SaveDataBlock(deletedBlock, metaInfDataStorage.HeadDataBlockList);
                SaveDataBlock(oldBlock, oldBlockOffset);
                SaveMetaInfStorage(metaInfDataStorage);
            }
        }
        public void SaveDataBlock(DataBlockNode block, int offset)
        {
            _fileStream.Seek(offset, SeekOrigin.Begin);
            //using var memStream = new MemoryStream();
            block = new DataBlockNode(block);
            ZeroFormatterSerializer.Serialize(_fileStream, block);
            //memStream.CopyTo(fs);
            _fileStream.Flush(true);
        }
        public void CreateAndAddDataBlock()
        {
            var metaInf = metaInfDataStorage;
            DataBlockNode newBlock;

            if (metaInf.HeadDataBlockList != 0)
            {
                var prevBlock = LoadDataBlock(metaInf.HeadDataBlockList);
                newBlock = new DataBlockNode(0, metaInf.HeadDataBlockList, CalculateDataBlockNodeSize());
                metaInf.HeadDataBlockList = (int)_fileStream.Seek(-GetCalculateMetaInfDataStorageSize(), SeekOrigin.End);
                prevBlock.PrevBlock = metaInf.HeadDataBlockList;
                SaveDataBlock(prevBlock, newBlock.NextBlock);
            }
            else
            {
                newBlock = new DataBlockNode(0, 0, CalculateDataBlockNodeSize());
                metaInf.HeadDataBlockList = (int)_fileStream.Seek(-GetCalculateMetaInfDataStorageSize(), SeekOrigin.End);
            }

            SaveDataBlock(newBlock, metaInf.HeadDataBlockList);
            CreateMetaInfInEnd(metaInf);
        }

        public DataBlockNode LoadDataBlock(int offset)
        {
            if (offset == 0)
            {
                return null;
            }

            _fileStream.Seek(offset, SeekOrigin.Begin);
            var buffer = new byte[metaInfDataStorage.DataBlockSize];
            _fileStream.Read(buffer, 0, metaInfDataStorage.DataBlockSize);

            using var memStream = new MemoryStream();
            memStream.Write(buffer, 0, buffer.Length);
            memStream.Seek(0, SeekOrigin.Begin);

            return ZeroFormatterSerializer.Deserialize<DataBlockNode>(memStream);
        }

        public DataBlockNode LoadHeadDataBlock() => LoadDataBlock(metaInfDataStorage.HeadDataBlockList);

        public TableFileManagerDataBlockNodeEnumerator GetBlockEnumarator() => new TableFileManagerDataBlockNodeEnumerator(this);

        public Table LoadTable()
        {
            var table = new Table();
            _fileStream.Seek(0, SeekOrigin.Begin);
            var rawTable = new byte[metaInfDataStorage.TableMetaInfSize];
            _fileStream.Read(rawTable, 0, rawTable.Length);
            table.TableMetaInf = ZeroFormatterSerializer.Deserialize<TableMetaInf>(rawTable);
            table.TableData = new DataStorageRowsInFiles(this);
            return table;
        }

        public void Dispose() => _fileStream.Dispose();
    }
}
