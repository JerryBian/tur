using System;
using System.Threading.Tasks;
using Tur.Extension;
using Xunit;

namespace Tur.Test.Extension;

public class TaskExtensionTest
{
    [Fact]
    public async Task Test_Case_1()
    {
        // Completed task passes through without throwing.
        await Task.CompletedTask.OkForCancel();
    }

    [Fact]
    public async Task Test_Case_2()
    {
        // Cancellation is swallowed.
        var tcs = new TaskCompletionSource<bool>();
        tcs.SetCanceled();
        await tcs.Task.OkForCancel();
    }

    [Fact]
    public async Task Test_Case_3()
    {
        // Non-cancellation exceptions still propagate.
        var tcs = new TaskCompletionSource<bool>();
        tcs.SetException(new InvalidOperationException("boom"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => tcs.Task.OkForCancel());
    }
}
