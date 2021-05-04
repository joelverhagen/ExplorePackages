﻿// <auto-generated />

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Knapcode.ExplorePackages;

namespace Knapcode.ExplorePackages.Worker.RunRealRestore
{
    /* Kusto DDL:

    .drop table RealRestoreResults ifexists;

    .create table RealRestoreResults (
        Timestamp: datetime,
        DotnetVersion: string,
        Duration: timespan,
        LowerId: string,
        Identity: string,
        Id: string,
        Version: string,
        Framework: string,
        Template: string,
        TargetCount: int,
        LibraryCount: int,
        RestoreSucceeded: bool,
        BuildSucceeded: bool,
        DependencyCount: int,
        FrameworkAssemblyCount: int,
        FrameworkReferenceCount: int,
        RuntimeAssemblyCount: int,
        ResourceAssemblyCount: int,
        CompileTimeAssemblyCount: int,
        NativeLibraryCount: int,
        BuildCount: int,
        BuildMultiTargetingCount: int,
        ContentFileCount: int,
        RuntimeTargetCount: int,
        ToolAssemblyCount: int,
        EmbedAssemblyCount: int,
        ErrorBlobPath: string,
        RestoreLogMessageCodes: dynamic,
        OnlyNU1202: bool,
        OnlyNU1213: bool,
        BuildErrorCodes: dynamic,
        OnlyMSB3644: bool
    );

    .alter-merge table RealRestoreResults policy retention softdelete = 30d;

    .alter table RealRestoreResults policy partitioning '{'
      '"PartitionKeys": ['
        '{'
          '"ColumnName": "Identity",'
          '"Kind": "Hash",'
          '"Properties": {'
            '"Function": "XxHash64",'
            '"MaxPartitionCount": 256'
          '}'
        '}'
      ']'
    '}';

    .create table RealRestoreResults ingestion csv mapping 'RealRestoreResults_mapping'
    '['
        '{"Column":"Timestamp","DataType":"datetime","Properties":{"Ordinal":0}},'
        '{"Column":"DotnetVersion","DataType":"string","Properties":{"Ordinal":1}},'
        '{"Column":"Duration","DataType":"timespan","Properties":{"Ordinal":2}},'
        '{"Column":"LowerId","DataType":"string","Properties":{"Ordinal":3}},'
        '{"Column":"Identity","DataType":"string","Properties":{"Ordinal":4}},'
        '{"Column":"Id","DataType":"string","Properties":{"Ordinal":5}},'
        '{"Column":"Version","DataType":"string","Properties":{"Ordinal":6}},'
        '{"Column":"Framework","DataType":"string","Properties":{"Ordinal":7}},'
        '{"Column":"Template","DataType":"string","Properties":{"Ordinal":8}},'
        '{"Column":"TargetCount","DataType":"int","Properties":{"Ordinal":9}},'
        '{"Column":"LibraryCount","DataType":"int","Properties":{"Ordinal":10}},'
        '{"Column":"RestoreSucceeded","DataType":"bool","Properties":{"Ordinal":11}},'
        '{"Column":"BuildSucceeded","DataType":"bool","Properties":{"Ordinal":12}},'
        '{"Column":"DependencyCount","DataType":"int","Properties":{"Ordinal":13}},'
        '{"Column":"FrameworkAssemblyCount","DataType":"int","Properties":{"Ordinal":14}},'
        '{"Column":"FrameworkReferenceCount","DataType":"int","Properties":{"Ordinal":15}},'
        '{"Column":"RuntimeAssemblyCount","DataType":"int","Properties":{"Ordinal":16}},'
        '{"Column":"ResourceAssemblyCount","DataType":"int","Properties":{"Ordinal":17}},'
        '{"Column":"CompileTimeAssemblyCount","DataType":"int","Properties":{"Ordinal":18}},'
        '{"Column":"NativeLibraryCount","DataType":"int","Properties":{"Ordinal":19}},'
        '{"Column":"BuildCount","DataType":"int","Properties":{"Ordinal":20}},'
        '{"Column":"BuildMultiTargetingCount","DataType":"int","Properties":{"Ordinal":21}},'
        '{"Column":"ContentFileCount","DataType":"int","Properties":{"Ordinal":22}},'
        '{"Column":"RuntimeTargetCount","DataType":"int","Properties":{"Ordinal":23}},'
        '{"Column":"ToolAssemblyCount","DataType":"int","Properties":{"Ordinal":24}},'
        '{"Column":"EmbedAssemblyCount","DataType":"int","Properties":{"Ordinal":25}},'
        '{"Column":"ErrorBlobPath","DataType":"string","Properties":{"Ordinal":26}},'
        '{"Column":"RestoreLogMessageCodes","DataType":"dynamic","Properties":{"Ordinal":27}},'
        '{"Column":"OnlyNU1202","DataType":"bool","Properties":{"Ordinal":28}},'
        '{"Column":"OnlyNU1213","DataType":"bool","Properties":{"Ordinal":29}},'
        '{"Column":"BuildErrorCodes","DataType":"dynamic","Properties":{"Ordinal":30}},'
        '{"Column":"OnlyMSB3644","DataType":"bool","Properties":{"Ordinal":31}}'
    ']'

    */
    partial record RealRestoreResult
    {
        public int FieldCount => 32;

        public void WriteHeader(TextWriter writer)
        {
            writer.WriteLine("Timestamp,DotnetVersion,Duration,LowerId,Identity,Id,Version,Framework,Template,TargetCount,LibraryCount,RestoreSucceeded,BuildSucceeded,DependencyCount,FrameworkAssemblyCount,FrameworkReferenceCount,RuntimeAssemblyCount,ResourceAssemblyCount,CompileTimeAssemblyCount,NativeLibraryCount,BuildCount,BuildMultiTargetingCount,ContentFileCount,RuntimeTargetCount,ToolAssemblyCount,EmbedAssemblyCount,ErrorBlobPath,RestoreLogMessageCodes,OnlyNU1202,OnlyNU1213,BuildErrorCodes,OnlyMSB3644");
        }

        public void Write(List<string> fields)
        {
            fields.Add(CsvUtility.FormatDateTimeOffset(Timestamp));
            fields.Add(DotnetVersion);
            fields.Add(Duration.ToString());
            fields.Add(LowerId);
            fields.Add(Identity);
            fields.Add(Id);
            fields.Add(Version);
            fields.Add(Framework);
            fields.Add(Template);
            fields.Add(TargetCount.ToString());
            fields.Add(LibraryCount.ToString());
            fields.Add(CsvUtility.FormatBool(RestoreSucceeded));
            fields.Add(CsvUtility.FormatBool(BuildSucceeded));
            fields.Add(DependencyCount.ToString());
            fields.Add(FrameworkAssemblyCount.ToString());
            fields.Add(FrameworkReferenceCount.ToString());
            fields.Add(RuntimeAssemblyCount.ToString());
            fields.Add(ResourceAssemblyCount.ToString());
            fields.Add(CompileTimeAssemblyCount.ToString());
            fields.Add(NativeLibraryCount.ToString());
            fields.Add(BuildCount.ToString());
            fields.Add(BuildMultiTargetingCount.ToString());
            fields.Add(ContentFileCount.ToString());
            fields.Add(RuntimeTargetCount.ToString());
            fields.Add(ToolAssemblyCount.ToString());
            fields.Add(EmbedAssemblyCount.ToString());
            fields.Add(ErrorBlobPath);
            fields.Add(RestoreLogMessageCodes);
            fields.Add(CsvUtility.FormatBool(OnlyNU1202));
            fields.Add(CsvUtility.FormatBool(OnlyNU1213));
            fields.Add(BuildErrorCodes);
            fields.Add(CsvUtility.FormatBool(OnlyMSB3644));
        }

        public void Write(TextWriter writer)
        {
            writer.Write(CsvUtility.FormatDateTimeOffset(Timestamp));
            writer.Write(',');
            CsvUtility.WriteWithQuotes(writer, DotnetVersion);
            writer.Write(',');
            writer.Write(Duration);
            writer.Write(',');
            CsvUtility.WriteWithQuotes(writer, LowerId);
            writer.Write(',');
            CsvUtility.WriteWithQuotes(writer, Identity);
            writer.Write(',');
            CsvUtility.WriteWithQuotes(writer, Id);
            writer.Write(',');
            CsvUtility.WriteWithQuotes(writer, Version);
            writer.Write(',');
            CsvUtility.WriteWithQuotes(writer, Framework);
            writer.Write(',');
            CsvUtility.WriteWithQuotes(writer, Template);
            writer.Write(',');
            writer.Write(TargetCount);
            writer.Write(',');
            writer.Write(LibraryCount);
            writer.Write(',');
            writer.Write(CsvUtility.FormatBool(RestoreSucceeded));
            writer.Write(',');
            writer.Write(CsvUtility.FormatBool(BuildSucceeded));
            writer.Write(',');
            writer.Write(DependencyCount);
            writer.Write(',');
            writer.Write(FrameworkAssemblyCount);
            writer.Write(',');
            writer.Write(FrameworkReferenceCount);
            writer.Write(',');
            writer.Write(RuntimeAssemblyCount);
            writer.Write(',');
            writer.Write(ResourceAssemblyCount);
            writer.Write(',');
            writer.Write(CompileTimeAssemblyCount);
            writer.Write(',');
            writer.Write(NativeLibraryCount);
            writer.Write(',');
            writer.Write(BuildCount);
            writer.Write(',');
            writer.Write(BuildMultiTargetingCount);
            writer.Write(',');
            writer.Write(ContentFileCount);
            writer.Write(',');
            writer.Write(RuntimeTargetCount);
            writer.Write(',');
            writer.Write(ToolAssemblyCount);
            writer.Write(',');
            writer.Write(EmbedAssemblyCount);
            writer.Write(',');
            CsvUtility.WriteWithQuotes(writer, ErrorBlobPath);
            writer.Write(',');
            CsvUtility.WriteWithQuotes(writer, RestoreLogMessageCodes);
            writer.Write(',');
            writer.Write(CsvUtility.FormatBool(OnlyNU1202));
            writer.Write(',');
            writer.Write(CsvUtility.FormatBool(OnlyNU1213));
            writer.Write(',');
            CsvUtility.WriteWithQuotes(writer, BuildErrorCodes);
            writer.Write(',');
            writer.Write(CsvUtility.FormatBool(OnlyMSB3644));
            writer.WriteLine();
        }

        public async Task WriteAsync(TextWriter writer)
        {
            await writer.WriteAsync(CsvUtility.FormatDateTimeOffset(Timestamp));
            await writer.WriteAsync(',');
            await CsvUtility.WriteWithQuotesAsync(writer, DotnetVersion);
            await writer.WriteAsync(',');
            await writer.WriteAsync(Duration.ToString());
            await writer.WriteAsync(',');
            await CsvUtility.WriteWithQuotesAsync(writer, LowerId);
            await writer.WriteAsync(',');
            await CsvUtility.WriteWithQuotesAsync(writer, Identity);
            await writer.WriteAsync(',');
            await CsvUtility.WriteWithQuotesAsync(writer, Id);
            await writer.WriteAsync(',');
            await CsvUtility.WriteWithQuotesAsync(writer, Version);
            await writer.WriteAsync(',');
            await CsvUtility.WriteWithQuotesAsync(writer, Framework);
            await writer.WriteAsync(',');
            await CsvUtility.WriteWithQuotesAsync(writer, Template);
            await writer.WriteAsync(',');
            await writer.WriteAsync(TargetCount.ToString());
            await writer.WriteAsync(',');
            await writer.WriteAsync(LibraryCount.ToString());
            await writer.WriteAsync(',');
            await writer.WriteAsync(CsvUtility.FormatBool(RestoreSucceeded));
            await writer.WriteAsync(',');
            await writer.WriteAsync(CsvUtility.FormatBool(BuildSucceeded));
            await writer.WriteAsync(',');
            await writer.WriteAsync(DependencyCount.ToString());
            await writer.WriteAsync(',');
            await writer.WriteAsync(FrameworkAssemblyCount.ToString());
            await writer.WriteAsync(',');
            await writer.WriteAsync(FrameworkReferenceCount.ToString());
            await writer.WriteAsync(',');
            await writer.WriteAsync(RuntimeAssemblyCount.ToString());
            await writer.WriteAsync(',');
            await writer.WriteAsync(ResourceAssemblyCount.ToString());
            await writer.WriteAsync(',');
            await writer.WriteAsync(CompileTimeAssemblyCount.ToString());
            await writer.WriteAsync(',');
            await writer.WriteAsync(NativeLibraryCount.ToString());
            await writer.WriteAsync(',');
            await writer.WriteAsync(BuildCount.ToString());
            await writer.WriteAsync(',');
            await writer.WriteAsync(BuildMultiTargetingCount.ToString());
            await writer.WriteAsync(',');
            await writer.WriteAsync(ContentFileCount.ToString());
            await writer.WriteAsync(',');
            await writer.WriteAsync(RuntimeTargetCount.ToString());
            await writer.WriteAsync(',');
            await writer.WriteAsync(ToolAssemblyCount.ToString());
            await writer.WriteAsync(',');
            await writer.WriteAsync(EmbedAssemblyCount.ToString());
            await writer.WriteAsync(',');
            await CsvUtility.WriteWithQuotesAsync(writer, ErrorBlobPath);
            await writer.WriteAsync(',');
            await CsvUtility.WriteWithQuotesAsync(writer, RestoreLogMessageCodes);
            await writer.WriteAsync(',');
            await writer.WriteAsync(CsvUtility.FormatBool(OnlyNU1202));
            await writer.WriteAsync(',');
            await writer.WriteAsync(CsvUtility.FormatBool(OnlyNU1213));
            await writer.WriteAsync(',');
            await CsvUtility.WriteWithQuotesAsync(writer, BuildErrorCodes);
            await writer.WriteAsync(',');
            await writer.WriteAsync(CsvUtility.FormatBool(OnlyMSB3644));
            await writer.WriteLineAsync();
        }

        public ICsvRecord ReadNew(Func<string> getNextField)
        {
            return new RealRestoreResult
            {
                Timestamp = CsvUtility.ParseDateTimeOffset(getNextField()),
                DotnetVersion = getNextField(),
                Duration = TimeSpan.Parse(getNextField()),
                LowerId = getNextField(),
                Identity = getNextField(),
                Id = getNextField(),
                Version = getNextField(),
                Framework = getNextField(),
                Template = getNextField(),
                TargetCount = CsvUtility.ParseNullable(getNextField(), int.Parse),
                LibraryCount = CsvUtility.ParseNullable(getNextField(), int.Parse),
                RestoreSucceeded = bool.Parse(getNextField()),
                BuildSucceeded = CsvUtility.ParseNullable(getNextField(), bool.Parse),
                DependencyCount = CsvUtility.ParseNullable(getNextField(), int.Parse),
                FrameworkAssemblyCount = CsvUtility.ParseNullable(getNextField(), int.Parse),
                FrameworkReferenceCount = CsvUtility.ParseNullable(getNextField(), int.Parse),
                RuntimeAssemblyCount = CsvUtility.ParseNullable(getNextField(), int.Parse),
                ResourceAssemblyCount = CsvUtility.ParseNullable(getNextField(), int.Parse),
                CompileTimeAssemblyCount = CsvUtility.ParseNullable(getNextField(), int.Parse),
                NativeLibraryCount = CsvUtility.ParseNullable(getNextField(), int.Parse),
                BuildCount = CsvUtility.ParseNullable(getNextField(), int.Parse),
                BuildMultiTargetingCount = CsvUtility.ParseNullable(getNextField(), int.Parse),
                ContentFileCount = CsvUtility.ParseNullable(getNextField(), int.Parse),
                RuntimeTargetCount = CsvUtility.ParseNullable(getNextField(), int.Parse),
                ToolAssemblyCount = CsvUtility.ParseNullable(getNextField(), int.Parse),
                EmbedAssemblyCount = CsvUtility.ParseNullable(getNextField(), int.Parse),
                ErrorBlobPath = getNextField(),
                RestoreLogMessageCodes = getNextField(),
                OnlyNU1202 = CsvUtility.ParseNullable(getNextField(), bool.Parse),
                OnlyNU1213 = CsvUtility.ParseNullable(getNextField(), bool.Parse),
                BuildErrorCodes = getNextField(),
                OnlyMSB3644 = CsvUtility.ParseNullable(getNextField(), bool.Parse),
            };
        }
    }
}
