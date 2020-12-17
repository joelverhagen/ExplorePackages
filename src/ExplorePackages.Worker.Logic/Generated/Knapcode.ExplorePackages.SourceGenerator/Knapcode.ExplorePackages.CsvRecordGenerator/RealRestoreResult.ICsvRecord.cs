﻿// <auto-generated />
using System;
using System.IO;
using Knapcode.ExplorePackages;

namespace Knapcode.ExplorePackages.Worker.RunRealRestore
{
    partial class RealRestoreResult
    {
        public void Write(TextWriter writer)
        {
            writer.Write(CsvUtility.FormatDateTimeOffset(Timestamp));
            writer.Write(',');
            CsvUtility.WriteWithQuotes(writer, DotnetVersion);
            writer.Write(',');
            writer.Write(Duration);
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
            writer.Write(RestoreSucceeded);
            writer.Write(',');
            writer.Write(BuildSucceeded);
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
            writer.Write(OnlyNU1202);
            writer.Write(',');
            writer.Write(OnlyNU1213);
            writer.Write(',');
            CsvUtility.WriteWithQuotes(writer, BuildErrorCodes);
            writer.Write(',');
            writer.Write(OnlyMSB3644);
            writer.WriteLine();
        }

        public void Read(Func<string> getNextField)
        {
            Timestamp = CsvUtility.ParseDateTimeOffset(getNextField());
            DotnetVersion = getNextField();
            Duration = TimeSpan.Parse(getNextField());
            Id = getNextField();
            Version = getNextField();
            Framework = getNextField();
            Template = getNextField();
            TargetCount = CsvUtility.ParseNullable(getNextField(), int.Parse);
            LibraryCount = CsvUtility.ParseNullable(getNextField(), int.Parse);
            RestoreSucceeded = bool.Parse(getNextField());
            BuildSucceeded = CsvUtility.ParseNullable(getNextField(), bool.Parse);
            DependencyCount = CsvUtility.ParseNullable(getNextField(), int.Parse);
            FrameworkAssemblyCount = CsvUtility.ParseNullable(getNextField(), int.Parse);
            FrameworkReferenceCount = CsvUtility.ParseNullable(getNextField(), int.Parse);
            RuntimeAssemblyCount = CsvUtility.ParseNullable(getNextField(), int.Parse);
            ResourceAssemblyCount = CsvUtility.ParseNullable(getNextField(), int.Parse);
            CompileTimeAssemblyCount = CsvUtility.ParseNullable(getNextField(), int.Parse);
            NativeLibraryCount = CsvUtility.ParseNullable(getNextField(), int.Parse);
            BuildCount = CsvUtility.ParseNullable(getNextField(), int.Parse);
            BuildMultiTargetingCount = CsvUtility.ParseNullable(getNextField(), int.Parse);
            ContentFileCount = CsvUtility.ParseNullable(getNextField(), int.Parse);
            RuntimeTargetCount = CsvUtility.ParseNullable(getNextField(), int.Parse);
            ToolAssemblyCount = CsvUtility.ParseNullable(getNextField(), int.Parse);
            EmbedAssemblyCount = CsvUtility.ParseNullable(getNextField(), int.Parse);
            ErrorBlobPath = getNextField();
            RestoreLogMessageCodes = getNextField();
            OnlyNU1202 = CsvUtility.ParseNullable(getNextField(), bool.Parse);
            OnlyNU1213 = CsvUtility.ParseNullable(getNextField(), bool.Parse);
            BuildErrorCodes = getNextField();
            OnlyMSB3644 = CsvUtility.ParseNullable(getNextField(), bool.Parse);
        }
    }
}