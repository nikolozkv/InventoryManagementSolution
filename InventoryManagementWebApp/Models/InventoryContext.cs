using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementWebApp.Models
{
    public class InventoryContext : DbContext
    {
        public InventoryContext(DbContextOptions<InventoryContext> options) : base(options) { }

        // ძირითადი ცხრილები DbSet:
        public DbSet<Company> Companies { get; set; }
        public DbSet<CompanyType> CompanyTypes { get; set; }
        public DbSet<Beverage> Beverages { get; set; }
        public DbSet<BeverageProductType> BeverageProductTypes { get; set; }
        public DbSet<BeverageCategory> BeverageCategories { get; set; }
        public DbSet<BeverageSubCategory> BeverageSubCategories { get; set; }
        public DbSet<BeverageColor> BeverageColors { get; set; }
        public DbSet<BeverageSweetnessLevel> BeverageSweetnessLevels { get; set; }
        public DbSet<BeverageBlendingType> BeverageBlendingTypes { get; set; }
        public DbSet<Barrel> Barrels { get; set; }
        public DbSet<Operation> Operations { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<OperationDefinition> OperationDefinitions { get; set; }
        public DbSet<DocumentType> DocumentTypes { get; set; }
        public DbSet<OperationMathType> OperationMathTypes { get; set; }
        public DbSet<OperationTargetType> OperationTargetTypes { get; set; }
        public DbSet<CompanyBarrelSummary> CompanyBarrelSummary { get; set; }
        public DbSet<BarrelViewModel> BarrelViewModels { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // View-ს არ აქვს Primary Key
            modelBuilder.Entity<CompanyBarrelSummary>().HasNoKey();

            // ✅ Companies View
            modelBuilder.Entity<Company>()
                .HasKey(c => c.CompanyID); // ეს იძლევა საშუალებას გამოიყენო Navigation-ები
            modelBuilder.Entity<Company>().ToView("Companies");


            // Many-to-Many კავშირი Beverages <-> BeverageBlendingTypes
            modelBuilder.Entity<Beverage>()
                .HasMany(b => b.BlendingTypes)
                .WithMany(bt => bt.Beverages)
                .UsingEntity(j => j.ToTable("Beverage_Blendings"));

            // Foreign Key-ების განსაზღვრა
            modelBuilder.Entity<Beverage>()
                .HasOne(b => b.ProductType)
                .WithMany()
                .HasForeignKey(b => b.ProductTypeID);

            modelBuilder.Entity<Beverage>()
                .HasOne(b => b.Category)
                .WithMany()
                .HasForeignKey(b => b.CategoryID);

            modelBuilder.Entity<Beverage>()
                .HasOne(b => b.SubCategory)
                .WithMany()
                .HasForeignKey(b => b.SubCategoryID);

            modelBuilder.Entity<Beverage>()
                .HasOne(b => b.Color)
                .WithMany()
                .HasForeignKey(b => b.ColorID);

            modelBuilder.Entity<Beverage>()
                .HasOne(b => b.Sweetness)
                .WithMany()
                .HasForeignKey(b => b.SweetnessID);

            modelBuilder.Entity<Beverage>()
                .ToTable(tb => tb.HasTrigger("trg_PreventBeverageDelete_Usage"));
            // 👆👆👆 ----------------------------- 👆👆👆

            modelBuilder.Entity<Barrel>()
                .ToTable(tb => tb.HasTrigger("TR_Barrels_UpdateDate"));
            // 👆 ------------------------ 👆

            modelBuilder.Entity<Barrel>()
                .HasOne(b => b.Beverage)
                .WithMany()
                .HasForeignKey(b => b.BeverageID);

            modelBuilder.Entity<Barrel>()
                .HasOne(b => b.Company)
                .WithMany()
                .HasForeignKey(b => b.CompanyID);

            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<OperationMathType>().HasKey(x => x.Code);

            modelBuilder.Entity<OperationTargetType>().HasKey(x => x.Code);

            modelBuilder.Entity<BarrelViewModel>().HasNoKey().ToView(null);

            modelBuilder.Entity<Operation>()
                .HasOne(o => o.OperationDefinition)
                .WithMany()  // თუ OperationDefinition-ს არ აქვს შესაბამისი კოლექცია
                .HasForeignKey(o => o.OperationDefID);
        }
    }
}
