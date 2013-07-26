﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Merchello.Core.Models
{
    public interface IInvoiceItemItemization
    {
        IEnumerable<IInvoiceItem> InvoiceItems { get; set; }

        decimal Total();
    }
}
