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
        private readonly int _pageSize;
        private readonly bool _needReadRaw;
        private bool _disposed;
        protected CsvConfiguration CsvConfiguration { get; set; }
        protected readonly Stream Stream;
        protected readonly Stream CountStream;
        protected readonly CsvReader CsvReader;
        protected string HeaderRaw;
        protected int? TotalCount;

        public bool HasMoreResults { get; private set; } = true;

        public CsvDataReader(Stream stream, ImportContext context, bool needReadRaw = false)
        {
            CsvConfiguration = GetConfiguration(context);

            Stream = stream;
            CsvReader = new CsvReader(new StreamReader(Stream), CsvConfiguration);
            CsvReader.Context.RegisterClassMap<TCsvClassMap>();

            _pageSize = Convert.ToInt32(context.ImportProfile.Settings.FirstOrDefault(x => x.Name == CsvSettings.PageSize.Name)?.Value ?? 50);
            _needReadRaw = needReadRaw;
        }

        public CsvDataReader(Stream stream, ImportContext context, CsvConfiguration csvConfiguration, bool needReadRaw = false)
        {
            CsvConfiguration = MergeWithDefaultConfig(csvConfiguration, context);

            Stream = stream;
            CsvReader = new CsvReader(new StreamReader(Stream), CsvConfiguration);
            CsvReader.Context.RegisterClassMap<TCsvClassMap>();

            _pageSize = Convert.ToInt32(context.ImportProfile.Settings.FirstOrDefault(x => x.Name == CsvSettings.PageSize.Name)?.Value ?? 50);
            _needReadRaw = needReadRaw;
        }

        public CsvDataReader(Stream stream, Stream countStream, ImportContext context, bool needReadRaw = false)
            : this(stream, context, needReadRaw)
        {
            CountStream = countStream;
        }

        public CsvDataReader(Stream stream, Stream countStream, ImportContext context, CsvConfiguration csvConfiguration, bool needReadRaw = false)
            : this(stream, context, csvConfiguration, needReadRaw)
        {
            CountStream = countStream;
        }

        public virtual async Task<int> GetTotalCountAsync(ImportContext context)
        {
            if (TotalCount.HasValue)
            {
                return TotalCount.Value;
            }

            Stream stream;
            bool leaveOpen;
            if (Stream.CanSeek)
            {
                stream = Stream;
                leaveOpen = true;
            }
            else
            {
                stream = CountStream ?? throw new InvalidOperationException("Count stream is not provided.");
                leaveOpen = false;
            }

            var streamPosition = 0L;
            if (stream.CanSeek)
            {
                streamPosition = stream.Position;
                stream.Seek(0, SeekOrigin.Begin);
            }

            using var csvReader = new CsvReader(new StreamReader(stream), CsvConfiguration, leaveOpen);

            await csvReader.ReadAsync();
            csvReader.ReadHeader();

            HeaderRaw = string.Join(csvReader.Configuration.Delimiter, csvReader.HeaderRecord);

            TotalCount = 0;

            while (await csvReader.ReadAsync())
            {
                TotalCount++;
            }

            if (stream.CanSeek)
            {
                stream.Seek(streamPosition, SeekOrigin.Begin);
            }

            return TotalCount.Value;
        }

        public virtual async Task<object[]> ReadNextPageAsync(ImportContext context)
        {
            var result = new List<object>();

            for (var i = 0; i < _pageSize && HasMoreResults; i++)
            {
                try
                {
                    HasMoreResults = await CsvReader.ReadAsync();
                    if (HasMoreResults)
                    {
                        var record = CsvReader.GetRecord<TCsvImportable>();
                        if (!_needReadRaw)
                        {
                            result.Add(record);
                        }
                        else
                        {
                            var rawRecord = CsvReader.Parser.RawRecord.TrimEnd('\r', '\n');
                            var row = CsvReader.Parser.Row;

                            result.Add(new CsvImportRecord<TCsvImportable>
                            {
                                Row = row,
                                RawHeader = HeaderRaw,
                                RawRecord = rawRecord,
                                Record = record,
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    context.ErrorCallback?.Invoke(new ErrorInfo
                    {
                        ErrorMessage = ex.Message,
                    });
                }
            }

            return result.ToArray();
        }

        protected virtual CsvConfiguration GetConfiguration(ImportContext context)
        {
            var csvConfiguration = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = context.ImportProfile.Settings.GetValue<string>(CsvSettings.Delimiter),
                PrepareHeaderForMatch = args =>
                {
                    var result = args.Header.ToLower();
                    return result;
                },
                Mode = CsvMode.RFC4180,
                BadDataFound = null,
                //TODO: Temporary disable since it cause false positive errors on CsvMapping when access to csv cell by name in custom mapping converters  args.Row["Name"]
                //BadDataFound = args =>
                //{
                //    var errorInfo = new ErrorInfo
                //    {
                //        ErrorMessage = $"Bad entry found at field {args.Field}",
                //        ErrorCode = "BadData",
                //        ErrorLine = args.Context.Parser.Row,
                //        RawHeader = string.Join(args.Context.Parser.Delimiter, args.Context.Reader.HeaderRecord),
                //        RawData = args.Context.Parser.RawRecord,
                //    };
                //    context.ErrorCallback(errorInfo);
                //}
            };

            if (context.ErrorCallback != null)
            {
                csvConfiguration.ReadingExceptionOccurred = args =>
                {
                    var errorInfo = new ErrorInfo
                    {
                        ErrorMessage = args.Exception.ToString(),
                        ErrorCode = "CsvHelperException"
                    };

                    context.ErrorCallback(errorInfo);
                    return false;
                };

                csvConfiguration.MissingFieldFound = args =>
                {
                    var errorInfo = new ErrorInfo
                    {
                        ErrorMessage = $"Headers with names {string.Join(", ", args.HeaderNames)} not found",
                        ErrorCode = "MissingFieldFound",
                        ErrorLine = args.Context.Parser.Row,
                        RawHeader = string.Join(args.Context.Parser.Delimiter, args.Context.Reader.HeaderRecord),
                        RawData = args.Context.Parser.RawRecord,
                    };

                    context.ErrorCallback(errorInfo);
                };
            }
            return csvConfiguration;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }
            if (disposing)
            {
                CsvReader.Dispose();
                Stream?.Dispose();
                CountStream?.Dispose();
            }
            _disposed = true;
        }

        private CsvConfiguration MergeWithDefaultConfig(CsvConfiguration csvConfiguration, ImportContext context)
        {
            var defaultCsvConfiguration = GetConfiguration(context);

            csvConfiguration.Delimiter ??= defaultCsvConfiguration.Delimiter;
            csvConfiguration.PrepareHeaderForMatch ??= defaultCsvConfiguration.PrepareHeaderForMatch;
            csvConfiguration.BadDataFound ??= defaultCsvConfiguration.BadDataFound;
            csvConfiguration.ReadingExceptionOccurred ??= defaultCsvConfiguration.ReadingExceptionOccurred;
            csvConfiguration.MissingFieldFound ??= defaultCsvConfiguration.MissingFieldFound;

            return csvConfiguration;
        }
    }
}
