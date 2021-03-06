﻿using reNX;
using System;
using System.Collections.Generic;
using System.IO;

namespace SharpEnd.Data
{
    internal sealed class ReactorDataProvider
    {
        private Dictionary<int, ReactorData> m_reactors;

        public ReactorDataProvider()
        {
            m_reactors = new Dictionary<int, ReactorData>();
        }

        public void Load()
        {
            using (NXFile file = new NXFile(Path.Combine("nx", "Reactor.nx")))
            {
                foreach (var node in file.BaseNode)
                {
                    int identifier = node.GetIdentifier<int>();

                    ReactorData item = new ReactorData();

                    item.Identifier = identifier;

                    m_reactors.Add(identifier, item);
                }
            }
                    }

        public ReactorData this[int identifier]
        {
            get
            {
                return m_reactors.GetOrDefault(identifier, null);
            }
        }
    }

    internal sealed class ReactorData
    {
        public int Identifier { get; set; }
    }
}