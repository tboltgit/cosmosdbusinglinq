///////////////////////////////////////
/////// Author : Amit Vishwakarma
///////////////////////////////////////  
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace DocumentDB
{
    public class DocumentDbRepository<T> : IDatabaseRepository<T>, IDisposable where T : class    
    {
        private readonly DocumentClient _client;
        private readonly string _databaseId;
        private readonly AsyncLazy<Database> _database;
        private readonly AsyncLazy<DocumentCollection> _collection;
        private readonly string _collectionName;

        #region Constructor
        public DocumentDbRepository(string endpointUrl, string authorizationKey, string databaseId, string collectionName)
        {
            if (string.IsNullOrWhiteSpace(endpointUrl))
                throw new ArgumentNullException(nameof(endpointUrl));

            if (string.IsNullOrWhiteSpace(authorizationKey))
                throw new ArgumentNullException(nameof(authorizationKey));

            _client = new DocumentClient(new Uri(endpointUrl), authorizationKey, new ConnectionPolicy());
            _databaseId = databaseId;
            _database = new AsyncLazy<Database>(async () => await CreateDatabaseAsync());
            _collection = new AsyncLazy<DocumentCollection>(async () => await CreateCollectionAsync());
            _collectionName = collectionName;
        }
        #endregion

        #region Public Methods
        public async Task<IQueryable<T>> GetAllAsync(Expression<Func<T, bool>> predicate, string partitionKey = null)
        {
            var link = UriFactory.CreateDocumentCollectionUri(_databaseId, _collectionName);

            IQueryable<T> querySetup = null;

            if (predicate == null)
            {
                querySetup = _client.CreateDocumentQuery<T>(
                       link,
                       new FeedOptions()
                       {
                           EnableCrossPartitionQuery = true,
                           MaxItemCount = int.MaxValue,
                           RequestContinuation = null,
                           PartitionKey = !String.IsNullOrEmpty(partitionKey) ? new PartitionKey(partitionKey) : null
                       });

            }
            else
            querySetup = _client.CreateDocumentQuery<T>(
                          link,
                          new FeedOptions()
                          {
                              EnableCrossPartitionQuery = true,
                              MaxItemCount = int.MaxValue,
                              RequestContinuation = null,
                              PartitionKey = !String.IsNullOrEmpty(partitionKey) ? new PartitionKey(partitionKey) : null
                          }).Where(predicate);

            var query = querySetup.AsDocumentQuery();

            return (await query.ExecuteNextAsync<T>()).AsQueryable();
        }

        public async Task<FeedResponse<T>> GetAllAsync(string sqlQuery, string token, int pageSize, string partitionKey = null)
        {           
            SqlQuerySpec sqlQuerySpec = new SqlQuerySpec();
            sqlQuerySpec.Parameters = new SqlParameterCollection();

            sqlQuerySpec.QueryText = sqlQuery;
            var link = UriFactory.CreateDocumentCollectionUri(_databaseId, _collectionName);

            IQueryable<T> querySetup = null;

            querySetup = _client.CreateDocumentQuery<T>(
                          link,
                          sqlQuerySpec,
                          new FeedOptions()
                          {
                              EnableCrossPartitionQuery = true,
                              MaxItemCount = int.MaxValue,
                              RequestContinuation = null,
                              PartitionKey = !String.IsNullOrEmpty(partitionKey) ? new PartitionKey(partitionKey) : null
                          });

            var query = querySetup.AsDocumentQuery();

            return (await query.ExecuteNextAsync<T>());
        }

        public async Task<FeedResponse<T>> GetManyWithPaginationAsync(string token, int pageSize,
            Expression<Func<T, bool>> predicate, string sortColumnOption, string sortOrder, string partitionKey = null)
        {
            var link = UriFactory.CreateDocumentCollectionUri(_databaseId, _collectionName);

            IQueryable<T> querySetup = null;
            IDocumentQuery<T> query = null;

            querySetup = _client.CreateDocumentQuery<T>(
                          link,
                          new FeedOptions()
                          {
                              EnableCrossPartitionQuery = true,
                              MaxItemCount = pageSize,
                              RequestContinuation = token,
                              PartitionKey = !String.IsNullOrEmpty(partitionKey) ? new PartitionKey(partitionKey) : null
                          });

            if (!String.IsNullOrWhiteSpace(sortColumnOption))
                querySetup = OrderByField(querySetup, sortColumnOption, sortOrder);

            if(predicate != null)
                querySetup = querySetup.Where(predicate);

            query = querySetup.AsDocumentQuery();

            return await query.ExecuteNextAsync<T>();
        }

        public async Task<string> AddAsync(T entity, string partitionKey)
        {
            var link = UriFactory.CreateDocumentCollectionUri(_databaseId, _collectionName);
            var createResponse = await _client.CreateDocumentAsync(link, entity);
            var createdDocument = createResponse.Resource;
            return createdDocument.Id;
        }

        public async Task<string> UpdateAsync(string id, T entity, string partitionKey, string etag)
        {
            var option = GetRequestOptions(partitionKey, etag);
            var updateResponse = await _client.ReplaceDocumentAsync(UriFactory.CreateDocumentUri(_databaseId, _collectionName, id), entity, option);
            var updated = updateResponse.Resource;
            return updated.Id;
        }

        public async Task<bool> DeleteAsync(string id, string partitionKey)
        {
            bool isSuccess = false;
            var doc = await GetDocumentById(id, partitionKey);

            if (doc != null)
            {
                var option = GetRequestOptions(partitionKey, null);
                var result = await _client.DeleteDocumentAsync(doc.SelfLink, option);
                isSuccess = result.StatusCode == HttpStatusCode.NoContent;
            }
            return isSuccess;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        #region Protected Methods
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // dispose managed resources
                _client.Dispose();
            }
        }
        #endregion

        #region Private Methods     
        private async Task<Document> GetDocumentById(object id, string partitionKey)
        {
            Expression<Func<Document, bool>> predicatequery = null;

            predicatequery = (doc => doc.Id == id.ToString());

            var link = UriFactory.CreateDocumentCollectionUri(_databaseId, _collectionName);

            IQueryable<Document> querySetup = null;

            querySetup = _client.CreateDocumentQuery<Document>(
                          link,
                          new FeedOptions()
                          {
                              EnableCrossPartitionQuery = true,
                              MaxItemCount = int.MaxValue,
                              RequestContinuation = null,
                              PartitionKey = !String.IsNullOrEmpty(partitionKey) ? new PartitionKey(partitionKey) : null
                          }).Where(predicatequery);

            var query = querySetup.AsDocumentQuery();

            return (await query.ExecuteNextAsync<Document>()).AsQueryable().FirstOrDefault();
        }

        private static RequestOptions GetRequestOptions(string partitionKey, string etag)
        {
            RequestOptions option = null;
            PartitionKey pk = null;
            AccessCondition ac = null;
            if (!string.IsNullOrWhiteSpace(partitionKey))
            {
                pk = new PartitionKey(partitionKey);
            }
            if (!string.IsNullOrWhiteSpace(etag))
            {
                // Using Access Conditions gives us the ability to use the ETag from our fetched document for optimistic concurrency.
                ac = new AccessCondition { Condition = etag, Type = AccessConditionType.IfMatch };
            }
            if (pk != null || ac != null)
            {
                option = new RequestOptions();
                if (pk != null)
                {
                    option.PartitionKey = pk;
                }
                if (ac != null)
                {
                    option.AccessCondition = ac;
                }
            }
            return option;
        }
        private async Task<DocumentCollection> CreateCollectionAsync()
        {
            DocumentCollection collection = _client.CreateDocumentCollectionQuery((await _database).SelfLink).Where(c => c.Id == _collectionName).FirstOrDefault();

            if (collection == null)
            {
                collection = new DocumentCollection { Id = _collectionName };

                collection = await _client.CreateDocumentCollectionAsync((await _database).SelfLink, collection);
            }

            return collection;
        }

        private async Task<Database> CreateDatabaseAsync()
        {
            Database database = _client.CreateDatabaseQuery()
                .Where(db => db.Id == _databaseId).FirstOrDefault();
            if (database == null)
            {
                database = await _client.CreateDatabaseAsync(
                    new Database { Id = _databaseId });
            }

            return database;
        }

        private IQueryable<T> OrderByField<T>(IQueryable<T> queryable, string fieldName, string sortOrder = "ASC")
        {
            var elementType = typeof(T);

            var orderByMethodName = sortOrder.ToUpper() == "ASC" ? "OrderBy" : "OrderByDescending";

            var parameterExpression = Expression.Parameter(elementType);
            var propertyOrFieldExpression =
                Expression.PropertyOrField(parameterExpression, fieldName);
            var selector = Expression.Lambda(propertyOrFieldExpression, parameterExpression);

            var orderByExpression = Expression.Call(typeof(Queryable), orderByMethodName,
                new[] { elementType, propertyOrFieldExpression.Type }, queryable.Expression, selector);

            return queryable.Provider.CreateQuery<T>(orderByExpression);
        }
        #endregion
    }

    public class AsyncLazy<T> : Lazy<Task<T>>
    {
        public AsyncLazy(Func<Task<T>> taskFactory) :
            base(() => Task.Factory.StartNew(taskFactory).Unwrap())
        {
        }

        public TaskAwaiter<T> GetAwaiter()
        {
            return Value.GetAwaiter();
        }
    }
}
