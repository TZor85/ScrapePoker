using Marten;
using Marten.Internal.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenScrape.Features.Table.GetAll
{
    public class GetAllTables
    {
        private IDocumentStore _documentStore;

        public GetAllTables(IDocumentStore documentStore)
        {
            _documentStore = documentStore;
        }
    }
}
