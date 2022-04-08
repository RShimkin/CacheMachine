using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;

namespace TestCache.Models
{
    [Index("Number")]
    public class Card
    {
        [Key]
        public string Number { get; set; }
        
        public string Pin { get; set; }

        public CardInfo? Info { get; set; }
    }
}
