using CSharpFunctionalExtensions;

namespace BackEncordados.Common.Utils;

public static class TapAsyncClass {
    public static async Task<Result<T, E>> TapAsync<T, E>(
        this Result<T, E> result,
        Func<T, Task> func){
        if (result.IsSuccess)
            await func(result.Value);

        return result;
    }
}