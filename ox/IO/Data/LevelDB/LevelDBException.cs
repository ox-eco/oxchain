using System.Data.Common;

namespace OX.IO.Data.LevelDB
{
    public class LevelDBException : DbException
    {
        internal LevelDBException(string message)
            : base(message)
        {
        }
    }
}
