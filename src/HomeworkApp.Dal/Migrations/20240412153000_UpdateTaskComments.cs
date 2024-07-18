using FluentMigrator;

namespace Route256.Week5.Workshop.PriceCalculator.Dal.Migrations;
[Migration(20240412153000, TransactionBehavior.None)]

public class UpdateTaskComments : Migration {
    public override void Up()
    {
        Alter.Table("task_comments")
            .AddColumn("modified_at").AsDateTimeOffset().Nullable()
            .AddColumn("deleted_at").AsDateTimeOffset().Nullable();
    }

    public override void Down()
    {
        Delete
            .Column("modified_at")
            .Column("deleted_at")
            .FromTable("task_comments");
    }
}