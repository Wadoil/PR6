using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1.Models
{
    internal class helper
    {
        private static Entities _context;
        /// <summary>
        /// Возвращает контекст базы данных
        /// </summary>
        /// <returns>Контекст базы данных</returns>
        public static Entities GetContext()
        {
            if (_context == null)
                _context = new Entities();
            return _context;
        }
    }
}
