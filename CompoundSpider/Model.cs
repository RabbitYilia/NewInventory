using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace CompoundSpider
{
    public class CompoundContext : DbContext
    {
        public DbSet<Compound> Compounds { get; set; }
        public DbSet<Setting> Settings { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=compound.db");
        }
    }

    public class Compound
    {
        public int ID { get; set; }
        public string CompoundName { get; set; }
        public string CAS { get; set; }
        public string SMILES { get; set; }
        public float MolecularWeight { get; set; }
        public string MolecularFormula { get; set; }
    }

    public class Setting
    {
        public int ID { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
    }
}