///////////////////////////////////////
/////// Author : Amit Vishwakarma
///////////////////////////////////////  
using Microsoft.Azure.Documents.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace DocumentDB
{
    public interface IDatabaseRepository<T> where T : class
    {
        Task<FeedResponse<T>> GetManyWithPaginationAsync(string token, int pageSize, Expression<Func<T, bool>> predicate, 
            string sortColumnOption, string sortOrder, string partitionKey = null);
        Task<IQueryable<T>> GetAllAsync(Expression<Func<T, bool>> predicate, string partitionKey = null);
        Task<FeedResponse<T>> GetAllAsync(string sqlQuery, string token, int pageSize, string partitionKey = null);
        Task<string> AddAsync(T entity, string partitionKey);
        Task<string> UpdateAsync(string id, T entity, string partitionKey, string etag);
        Task<bool> DeleteAsync(string id, string partitionKey);
        void Dispose();
    }
}
