namespace HotelListing.API.Contracts
{
    public interface IGenericRepository<T> where T : class
    {
        Task<T> GetAllAsync();
        Task<T> GetAsync(int id);

        Task UpdateAsync(T entity);
        Task DeleteAsync(int id);
        Task<T> AddSync(T entity);

        Task<bool> Exists(int id);
    }
}
