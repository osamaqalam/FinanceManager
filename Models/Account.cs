using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersonalFinanceManager.Models
{
    public class Account
    {
        public int AccountId { get; set; }
        public int UserId { get; set; }
        public string AccountName { get; set; }
        public decimal Balance { get; set; }
    }
}