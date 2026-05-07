namespace BackEncordados.Materials.Repository;

public interface IProductsRepository<T,F>
{
    Task<(IEnumerable<T> Items, int TotalCount)> FindAllAsync(F filter);
    Task<T?> FindByIdAsync(long id);
    Task<T?> FindByNameAsync(string name);
    Task<T?> UpdateAsync(T item,long id);
    Task<bool> DeleteAsync(long id);
    Task<T> CreateAsync(T item);
}