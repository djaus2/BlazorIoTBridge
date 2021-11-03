using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlazorIoTBridge.Shared;
using BlazorIoTBridge.Server.Data;

namespace BlazorIoTBridge.Server.Data
{
    public class ApplicationDBContext: DbContext
    {
        public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options, IDataAccessService _dataaccesservice) : base(options)
        {
        }
        public DbSet<Info> Infos { get; set; }
    }
}
