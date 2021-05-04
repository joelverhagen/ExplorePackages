﻿// <auto-generated />

using System.Collections.Generic;

namespace Knapcode.ExplorePackages
{
    static partial class KustoDDL
    {
        public const string NuGetPackageExplorerRecordDefaultTableName = "NuGetPackageExplorers";

        public static readonly IReadOnlyList<string> NuGetPackageExplorerRecordDDL = new[]
        {
            ".drop table __TABLENAME__ ifexists",

            @".create table __TABLENAME__ (
    LowerId: string,
    Identity: string,
    Id: string,
    Version: string,
    CatalogCommitTimestamp: datetime,
    Created: datetime,
    ResultType: string,
    SourceLinkResult: string,
    DeterministicResult: string,
    CompilerFlagsResult: string,
    IsSignedByAuthor: bool
)",

            ".alter-merge table __TABLENAME__ policy retention softdelete = 30d",

            @".alter table __TABLENAME__ policy partitioning '{'
  '""PartitionKeys"": ['
    '{'
      '""ColumnName"": ""Identity"",'
      '""Kind"": ""Hash"",'
      '""Properties"": {'
        '""Function"": ""XxHash64"",'
        '""MaxPartitionCount"": 256'
      '}'
    '}'
  ']'
'}'",

            @".create table __TABLENAME__ ingestion csv mapping 'BlobStorageMapping'
'['
    '{""Column"":""LowerId"",""DataType"":""string"",""Properties"":{""Ordinal"":2}},'
    '{""Column"":""Identity"",""DataType"":""string"",""Properties"":{""Ordinal"":3}},'
    '{""Column"":""Id"",""DataType"":""string"",""Properties"":{""Ordinal"":4}},'
    '{""Column"":""Version"",""DataType"":""string"",""Properties"":{""Ordinal"":5}},'
    '{""Column"":""CatalogCommitTimestamp"",""DataType"":""datetime"",""Properties"":{""Ordinal"":6}},'
    '{""Column"":""Created"",""DataType"":""datetime"",""Properties"":{""Ordinal"":7}},'
    '{""Column"":""ResultType"",""DataType"":""string"",""Properties"":{""Ordinal"":8}},'
    '{""Column"":""SourceLinkResult"",""DataType"":""string"",""Properties"":{""Ordinal"":9}},'
    '{""Column"":""DeterministicResult"",""DataType"":""string"",""Properties"":{""Ordinal"":10}},'
    '{""Column"":""CompilerFlagsResult"",""DataType"":""string"",""Properties"":{""Ordinal"":11}},'
    '{""Column"":""IsSignedByAuthor"",""DataType"":""bool"",""Properties"":{""Ordinal"":12}}'
']'",
        };

        private static readonly bool NuGetPackageExplorerRecordAddTypeToDefaultTableName = AddTypeToDefaultTableName(typeof(Knapcode.ExplorePackages.Worker.NuGetPackageExplorerToCsv.NuGetPackageExplorerRecord), NuGetPackageExplorerRecordDefaultTableName);

        private static readonly bool NuGetPackageExplorerRecordAddTypeToDDL = AddTypeToDDL(typeof(Knapcode.ExplorePackages.Worker.NuGetPackageExplorerToCsv.NuGetPackageExplorerRecord), NuGetPackageExplorerRecordDDL);
    }
}
