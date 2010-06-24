﻿
using System;
using Orchard.DataMigration.Interpreters;
using Orchard.DataMigration.Schema;

public class NullInterpreter : IDataMigrationInterpreter {
    public void Visit(SchemaCommand command) {
    }

    public void Visit(CreateTableCommand command) {
    }

    public void Visit(DropTableCommand command) {
    }

    public void Visit(AlterTableCommand command) {
    }

    public void Visit(SqlStatementCommand command) {
    }

    public void Visit(CreateForeignKeyCommand command) {
    }

    public void Visit(DropForeignKeyCommand command) {
    }
}