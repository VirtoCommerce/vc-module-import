using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using VirtoCommerce.ImportModule.Core.Models;
using VirtoCommerce.ImportModule.Core.Services;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.ImportModule.CsvHelper
{
    public class CsvDataReader<TCsvImportable, TCsvClassMap> : IImportDataReader where TCsvClassMap : ClassMap
    {
        private int? _totalCount;
        private int _pageSize;
        private string _headerRaw;
        private readonly Stream _stream;
        protected CsvConfiguration CsvConfiguration { get; set; }
        private readonly CsvReader _csvReader;
        private readonly bool _needReadRaw;

        public bool HasMoreResults { get; private set; } = true;

        public CsvDataReader(Stream stream, ImportContext context, bool needReadRaw = false)
        {
            CsvConfiguration = GetConfiguration(context);

            _stream = stream;
            _csvReader = new CsvReader(new StreamReader(_stream), CsvConfiguration);
            _csvReader.Context.RegisterClassMap<TCsvClassMap>();

            _pageSize = Convert.ToInt32(context.ImportProfile.Settings.FirstOrDefault(x => x.Name == CsvSettings.PageSize.Name)?.Value ?? 50);
            _needReadRaw = needReadRaw;
        }

        public async Task<int> GetTotalCountAsync(ImportContext context)
        {
            if (_totalCount.HasValue)
            {
                return _totalCount.Value;
            }

            var streamPosition = _stream.Position;
            _stream.Seek(0, SeekOrigin.Begin);

            var streamReader = new StreamReader(_stream, leaveOpen: true);
            var csvReader = new CsvReader(streamReader, CsvConfiguration);

            await csvReader.ReadAsync();
            csvReader.ReadHeader();

            _headerRaw = string.Join(csvReader.Configuration.Delimiter, csvReader.HeaderRecord);

            _totalCount = 0;

            while (await csvReader.ReadAsync())
            {
                _totalCount++;
            }

            _stream.Seek(streamPosition, SeekOrigin.Begin);

            return _totalCount.Value;
        }

        public async Task<object[]> ReadNextPageAsync(ImportContext context)
        {
            var result = new List<object>();

            for (var i = 0; i < _pageSize && HasMoreResults; i++)
            {
                HasMoreResults = await _csvReader.ReadAsync();
                if (HasMoreResults)
                {
                    var record = _csvReader.GetRecord<TCsvImportable>();
                    if (!_needReadRaw)
                    {
                        result.Add(record);
                    }
                    else
                    {
                        var rawRecord = _csvReader.Parser.RawRecord.TrimEnd('\r', '\n');
                        var row = _csvReader.Parser.Row;

                        result.Add(new ImportRecord<TCsvImportable>
                        {
                            Row = row,
                            RawHeader = _headerRaw,
                            RawRecord = rawRecord,
                            Record = record
                        });
                    }
                }
            }

            return result.ToArray();
        }

        protected virtual CsvConfiguration GetConfiguration(ImportContext context)
        {
            var csvConfigurarion = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = context.ImportProfile.Settings.GetSettingValue(CsvSettings.Delimiter.Name, (string)CsvSettings.Delimiter.DefaultValue),
                PrepareHeaderForMatch = (PrepareHeaderForMatchArgs args) =>
                {
                    var result = args.Header.ToLower();
                    return result;
                },
                Mode = CsvMode.NoEscape,
                BadDataFound = args =>
                {
                    var errorInfo = new ErrorInfo
                    {
                        ErrorMessage = $"Bad entry found at field {args.Field}",
                        ErrorCode = "BadData",
                        ErrorLine = args.Context.Parser.Row,
                        RawHeader = string.Join(args.Context.Parser.Delimiter, args.Context.Reader.HeaderRecord),
                        RawData = args.Context.Parser.RawRecord,
                    };
                    context.ErrorCallback(errorInfo);
                }
            };

            if (context.ErrorCallback != null)
            {
                csvConfigurarion.ReadingExceptionOccurred = args =>
                {
                    var errorInfo = new ErrorInfo
                    {
                        ErrorMessage = args.Exception.ToString(),
                        ErrorCode = "CsvHelperException"
                    };

                    context.ErrorCallback(errorInfo);
                    return false;
                };

                csvConfigurarion.MissingFieldFound = args =>
                {
                    var errorInfo = new ErrorInfo
                    {
                        ErrorMessage = "This row has either an unclosed quote or missed column",
                        ErrorCode = "MissingFieldFound",
                        ErrorLine = args.Context.Parser.Row,
                        RawHeader = string.Join(args.Context.Parser.Delimiter, args.Context.Reader.HeaderRecord),
                        RawData = args.Context.Parser.RawRecord,
                    };

                    context.ErrorCallback(errorInfo);
                };
            }
            return csvConfigurarion;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            _csvReader.Dispose();
            _stream?.Dispose();
        }
    }
}
