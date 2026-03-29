using Microsoft.Data.Sqlite;

namespace TurboScraper;

public sealed class SeenCarsRepository : IDisposable
{
    private readonly SqliteConnection _connection;

    public SeenCarsRepository(string dbPath = "turbo_scraper.db")
    {
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath
        }.ToString();

        _connection = new SqliteConnection(connectionString);
        _connection.Open();

        EnablePragmas();
        Initialize();
    }

    private void EnablePragmas()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText =
        @"
        PRAGMA journal_mode = WAL;
        PRAGMA synchronous = NORMAL;
        ";
        cmd.ExecuteNonQuery();
    }

    private void Initialize()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText =
        @"
        CREATE TABLE IF NOT EXISTS SeenCars (
            Id INTEGER PRIMARY KEY,
            Url TEXT NOT NULL,
            FirstSeenAt TEXT NOT NULL,
            LastSeenAt TEXT NOT NULL
        );

        CREATE INDEX IF NOT EXISTS IX_SeenCars_LastSeenAt
        ON SeenCars(LastSeenAt);
        ";
        cmd.ExecuteNonQuery();
    }

    public bool TryInsert(CarModel car)
    {
        var now = DateTime.UtcNow.ToString("O");

        using var cmd = _connection.CreateCommand();
        cmd.CommandText =
        @"
        INSERT OR IGNORE INTO SeenCars (Id, Url, FirstSeenAt, LastSeenAt)
        VALUES ($id, $url, $firstSeenAt, $lastSeenAt);
        ";

        cmd.Parameters.AddWithValue("$id", car.Id);
        cmd.Parameters.AddWithValue("$url", car.Url);
        cmd.Parameters.AddWithValue("$firstSeenAt", now);
        cmd.Parameters.AddWithValue("$lastSeenAt", now);

        return cmd.ExecuteNonQuery() > 0;
    }

    public void Touch(int id)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText =
        @"
        UPDATE SeenCars
        SET LastSeenAt = $lastSeenAt
        WHERE Id = $id;
        ";

        cmd.Parameters.AddWithValue("$id", id);
        cmd.Parameters.AddWithValue("$lastSeenAt", DateTime.UtcNow.ToString("O"));

        cmd.ExecuteNonQuery();
    }

    public void DeleteOlderThan(DateTime utcCutoff)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText =
        @"
        DELETE FROM SeenCars
        WHERE LastSeenAt < $cutoff;
        ";

        cmd.Parameters.AddWithValue("$cutoff", utcCutoff.ToString("O"));
        cmd.ExecuteNonQuery();
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}