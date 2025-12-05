using System;
using System.Diagnostics;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace TMech.Sharp.SqliteService
{
    public interface IDbWriteRequest
    {
        public Task Execute();
    }

    public sealed record DbWriteRequest<T>: IDbWriteRequest
    {
        public DbWriteRequest(SQLiteWriteAction<T> action)
        {
            Action = action;
        }

        public SQLiteWriteAction<T> Action { get; }
        public TaskCompletionSource<T> Completion { get; } = new();

        public Task Execute()
        {
            try
            {
                T result = Action();
                Completion.SetResult(result);
            }
            catch (Exception error)
            {
                Completion.SetException(error);
            }

            return Task.CompletedTask;
        }
    }

    public delegate T SQLiteWriteAction<T>();

    public static class SQLiteConcurrentWriter
    {
        private static bool _isStarted = false;
        private static readonly ChannelWriter<IDbWriteRequest> _writer;
        private static readonly ChannelReader<IDbWriteRequest> _reader;

        static SQLiteConcurrentWriter()
        {
            var channel = Channel.CreateUnbounded<IDbWriteRequest>();
            _reader = channel.Reader;
            _writer = channel.Writer;
        }

        public static bool Running => _isStarted;

        public static void Start()
        {
            if (_isStarted) throw new InvalidOperationException($"{nameof(SQLiteConcurrentWriter)} has already been started");
            _isStarted = true;

            Task.Run(async () =>
            {
                await foreach (IDbWriteRequest current in _reader.ReadAllAsync())
                {
                    await current.Execute();
                }
            });
        }

        public static async Task<T> EnqueueWrite<T>(SQLiteWriteAction<T> action)
        {
            var request = new DbWriteRequest<T>(action);
            var result = _writer.TryWrite(request);
            Debug.Assert(result == true);
            return await request.Completion.Task;
        }

        public static void Stop()
        {
            if (!_isStarted) return;
            _isStarted = false;

            _writer.Complete();
            _reader.Completion.GetAwaiter().GetResult();
        }
    }
}
