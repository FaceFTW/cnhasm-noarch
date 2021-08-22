using System;
using System.Collections.Generic;
using System.Text;

namespace chnasm {
    public class SymbolTable {
        private List<Symbol> _symList;

        public SymbolTable() {
            this._symList = new List<Symbol>();
        }

        public void AddSymbol(Symbol _sym) {
            this._symList.Add(_sym);
        }

        public ushort GetSymbolAddr(string _name) {

            return this._symList.Find(_s => _s.Name == _name).Address;
        }
    }
}