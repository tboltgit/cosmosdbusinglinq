using System.Linq;

namespace DocumentDB.Model
{
    public class PaginatedResponse<T> where T : class
    {
        private string _continuationToken = null;
        private IQueryable<T> _resultSet = null;

        public PaginatedResponse(IQueryable<T> resultSet, string continuationToken)
        {
            _resultSet = resultSet;
            _continuationToken = continuationToken;

        }

        public IQueryable<T> ResultSet { get { return _resultSet; } }
        public string ContinuationToken { get { return _continuationToken; } }
    }
}
