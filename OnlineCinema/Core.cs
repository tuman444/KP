using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineCinema
{
    public class Core
    {
        private static OnlineCinemaDBEntities _db;

        public static OnlineCinemaDBEntities DB => GetContext();

        public static OnlineCinemaDBEntities GetContext()
        {
            if (_db == null)
            {
                _db = new OnlineCinemaDBEntities();
            }
            return _db;
        }
    }
}
