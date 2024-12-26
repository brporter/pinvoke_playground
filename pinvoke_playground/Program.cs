using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks.Sources;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace pinvoke_playground;

public partial class NativeInterop
{
    public enum CallbackResult
        : int
    {
        Complete = 0,
        Pending = 1,
    }

    [LibraryImport("callback_test_native_lib", EntryPoint = "hello")]
    public static partial int Hello(int input);

    [LibraryImport("callback_test_native_lib", EntryPoint = "invokeCallback")]
    public static unsafe partial CallbackResult InvokeCallback(delegate* unmanaged[Cdecl]<int, IntPtr, void> callback, IntPtr state, [MarshalAs(UnmanagedType.Bool)] bool async);
}

public class ManualResetValueTaskSource<T>
    : IValueTaskSource<T>
{
    private ManualResetValueTaskSourceCore<T> _valueSource = new();
    
    public ManualResetValueTaskSource()
    {
        Reset();
    }

    public ValueTask<T> Task { get; private set; }
    public void SetResult(T result) => _valueSource.SetResult(result);

    public void SetException(Exception error) => _valueSource.SetException(error);
    public void Reset() 
    {
        _valueSource.Reset();
        Task = new ValueTask<T>(this, this.Version);
    }
    public short Version => _valueSource.Version;

    public ValueTaskSourceStatus GetStatus(short token) => _valueSource.GetStatus(token);

    public T GetResult(short token) => _valueSource.GetResult(token);
   
    public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) => _valueSource.OnCompleted(continuation, state, token, flags);
}

public class TestClassValueTaskSource
{
    readonly ManualResetValueTaskSource<int> _valueSource = new();
    public unsafe ValueTask<int> InvokeCallbackAsync(bool nativeAsync)
    {
        _valueSource.Reset();
        GCHandle handle = GCHandle.Alloc(_valueSource);

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static void CallbackFunction(int input, IntPtr state)
        {
            GCHandle handle = GCHandle.FromIntPtr(state);
            var vcs = (ManualResetValueTaskSource<int>)handle.Target!;
            handle.Free();

            vcs.SetResult(input);
        }
        
        delegate* unmanaged[Cdecl]<int, IntPtr, void> callbackFunction = &CallbackFunction;
        
        if (NativeInterop.CallbackResult.Complete == NativeInterop.InvokeCallback(callbackFunction, GCHandle.ToIntPtr(handle), nativeAsync))
        {
            return ValueTask.FromResult<int>(_valueSource.GetResult(_valueSource.Version));
        }
        
        return new ValueTask<int>(_valueSource, _valueSource.Version);
    }
    
    public unsafe ValueTask<int> InvokeCallbackAsync2(bool nativeAsync)
    {
        _valueSource.Reset();
        GCHandle handle = GCHandle.Alloc(_valueSource);

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static void CallbackFunction(int input, IntPtr state)
        {
            GCHandle handle = GCHandle.FromIntPtr(state);
            var vcs = (ManualResetValueTaskSource<int>)handle.Target!;
            handle.Free();

            vcs.SetResult(input);
        }
        
        delegate* unmanaged[Cdecl]<int, IntPtr, void> callbackFunction = &CallbackFunction;

        NativeInterop.InvokeCallback(callbackFunction, GCHandle.ToIntPtr(handle), nativeAsync);
        
        return new ValueTask<int>(_valueSource, _valueSource.Version);
    }   
    
    public unsafe ValueTask<int> InvokeCallbackAsync3(bool nativeAsync)
    {
        _valueSource.Reset();
        GCHandle handle = GCHandle.Alloc(_valueSource);

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static void CallbackFunction(int input, IntPtr state)
        {
            GCHandle handle = GCHandle.FromIntPtr(state);
            var vcs = (ManualResetValueTaskSource<int>)handle.Target!;
            handle.Free();

            vcs.SetResult(input);
        }
        
        delegate* unmanaged[Cdecl]<int, IntPtr, void> callbackFunction = &CallbackFunction;

        NativeInterop.InvokeCallback(callbackFunction, GCHandle.ToIntPtr(handle), nativeAsync);

        return _valueSource.Task;
    }
}

public class TestClassTaskCompletionSource
{
    public unsafe Task<int> InvokeCallbackAsync(bool nativeAsync)
    {
        var tcs = new TaskCompletionSource<int>();
        GCHandle handle = GCHandle.Alloc(tcs);

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static void CallbackFunction(int input, IntPtr state)
        {
            GCHandle handle = GCHandle.FromIntPtr(state);

            var tcs = (TaskCompletionSource<int>)handle.Target!;

            handle.Free();
            tcs.SetResult(input);
        }
        
        delegate* unmanaged[Cdecl]<int, IntPtr, void> callbackFunction = &CallbackFunction;

        NativeInterop.InvokeCallback(callbackFunction, GCHandle.ToIntPtr(handle), nativeAsync);
        
        return tcs.Task;
    }
}

[MemoryDiagnoser]
public class Benchmarks
{
    private readonly TestClassValueTaskSource _valueTaskSource = new();
    private readonly TestClassTaskCompletionSource _taskCompletionSource = new();
    
    [Benchmark]
    public async ValueTask<int> ValueTaskSourceAsync()
    {
        return await _valueTaskSource.InvokeCallbackAsync(true);
    }
    
    [Benchmark]
    public async ValueTask<int> ValueTaskSourceAsyncPreAllocatedTask()
    {
        return await _valueTaskSource.InvokeCallbackAsync3(true);
    }
    
    [Benchmark]
    public async ValueTask<int> ValueTaskSourceSyncFromResult()
    {
        return await _valueTaskSource.InvokeCallbackAsync(false);
    }
    
    [Benchmark]
    public async ValueTask<int> ValueTaskSourceSyncAlwaysNewValueTask()
    {
        return await _valueTaskSource.InvokeCallbackAsync2(false);
    }
    
    [Benchmark]
    public async Task<int> TaskCompletionSourceAsync()
    {
        return await _taskCompletionSource.InvokeCallbackAsync(true);
    }
    
    [Benchmark]
    public async Task<int> TaskCompletionSourceSync()
    {
        return await _taskCompletionSource.InvokeCallbackAsync(false);
    }
}

class Program
{
    static void Main(string[] args)
    {
        BenchmarkRunner.Run<Benchmarks>();
    }
}