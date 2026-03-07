using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<ContaGrafica> ContasGraficas { get; set; }
        public DbSet<Custodia> Custodias { get; set; }
        public DbSet<Distribuicao> Distribuicoes { get; set; }
        public DbSet<CestaRecomendacao> CestasRecomendacao { get; set; }
        public DbSet<ItemCesta> ItensCesta { get; set; }
        public DbSet<OrdemCompra> OrdensCompra { get; set; }
        public DbSet<EventoIR> EventosIR { get; set; }
        public DbSet<Cotacao> Cotacoes { get; set; }
        public DbSet<Rebalanceamento> Rebalanceamentos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- CLIENTES ---
            modelBuilder.Entity<Cliente>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nome).HasMaxLength(200).IsRequired();
                entity.Property(e => e.Cpf).HasMaxLength(11).IsRequired();
                entity.HasIndex(e => e.Cpf).IsUnique();
                entity.Property(e => e.Email).HasMaxLength(200).IsRequired();
                entity.Property(e => e.ValorMensal).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Ativo).HasDefaultValue(true);
                
                // Relacionamentos 1:N
                entity.HasMany(c => c.ContasGraficas).WithOne(cg => cg.Cliente).HasForeignKey(cg => cg.ClienteId);
                entity.HasMany(c => c.EventosIR).WithOne(e => e.Cliente).HasForeignKey(e => e.ClienteId);
                entity.HasMany(c => c.Rebalanceamentos).WithOne(r => r.Cliente).HasForeignKey(r => r.ClienteId);
            });

            // --- CONTAS GRAFICAS ---
            modelBuilder.Entity<ContaGrafica>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.NumeroConta).HasMaxLength(20).IsRequired();
                entity.HasIndex(e => e.NumeroConta).IsUnique();
                entity.Property(e => e.Tipo).HasConversion<string>().IsRequired(); // Salva o Enum como string (MASTER, FILHOTE)

                entity.HasMany(cg => cg.Custodias).WithOne(c => c.ContaGrafica).HasForeignKey(c => c.ContaGraficaId);
                entity.HasMany(cg => cg.OrdensCompra).WithOne(oc => oc.ContaMaster).HasForeignKey(oc => oc.ContaMasterId);
            });

            // --- CUSTODIAS ---
            modelBuilder.Entity<Custodia>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Ticker).HasMaxLength(10).IsRequired();
                entity.Property(e => e.PrecoMedio).HasColumnType("decimal(18,4)");

                entity.HasMany(c => c.Distribuicoes).WithOne(d => d.CustodiaFilhote).HasForeignKey(d => d.CustodiaFilhoteId);
            });

            // --- ORDENS DE COMPRA ---
            modelBuilder.Entity<OrdemCompra>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Ticker).HasMaxLength(10).IsRequired();
                entity.Property(e => e.PrecoUnitario).HasColumnType("decimal(18,4)");
                entity.Property(e => e.TipoMercado).HasConversion<string>().IsRequired();

                entity.HasMany(oc => oc.Distribuicoes).WithOne(d => d.OrdemCompra).HasForeignKey(d => d.OrdemCompraId);
            });

            // --- DISTRIBUICOES ---
            modelBuilder.Entity<Distribuicao>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Ticker).HasMaxLength(10).IsRequired();
                entity.Property(e => e.PrecoUnitario).HasColumnType("decimal(18,4)");
            });

            // --- CESTA RECOMENDACAO ---
            modelBuilder.Entity<CestaRecomendacao>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nome).HasMaxLength(100).IsRequired();

                entity.HasMany(c => c.Itens).WithOne(i => i.Cesta).HasForeignKey(i => i.CestaId);
            });

            // --- ITENS DA CESTA ---
            modelBuilder.Entity<ItemCesta>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Ticker).HasMaxLength(10).IsRequired();
                entity.Property(e => e.Percentual).HasColumnType("decimal(5,2)");
            });

            // --- EVENTOS IR ---
            modelBuilder.Entity<EventoIR>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Tipo).HasConversion<string>().IsRequired();
                entity.Property(e => e.ValorBase).HasColumnType("decimal(18,2)");
                entity.Property(e => e.ValorIR).HasColumnType("decimal(18,2)");
            });

            // --- COTACOES ---
            modelBuilder.Entity<Cotacao>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.DataPregao).HasColumnType("date");
                entity.Property(e => e.Ticker).HasMaxLength(10).IsRequired();
                entity.Property(e => e.PrecoAbertura).HasColumnType("decimal(18,4)");
                entity.Property(e => e.PrecoFechamento).HasColumnType("decimal(18,4)");
                entity.Property(e => e.PrecoMaximo).HasColumnType("decimal(18,4)");
                entity.Property(e => e.PrecoMinimo).HasColumnType("decimal(18,4)");
            });

            // --- REBALANCEAMENTOS ---
            modelBuilder.Entity<Rebalanceamento>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Tipo).HasConversion<string>().IsRequired();
                entity.Property(e => e.TickerVendido).HasMaxLength(10).IsRequired();
                entity.Property(e => e.TickerComprado).HasMaxLength(10).IsRequired();
                entity.Property(e => e.ValorVenda).HasColumnType("decimal(18,2)");
            });
        }
    }
}