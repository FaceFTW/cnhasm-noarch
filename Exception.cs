using System;
using System.Collections.Generic;
using System.Text;

namespace chnasm {
    public class InvalidOpExcepiton : Exception {

        private string _opToken;
        private ushort _opAddr;

        protected InvalidOpExcepiton() : base() { }

        public InvalidOpExcepiton(String _token, ushort _addr) : base(
            $"Instruction at Address {_addr:X} uses Invalid/Unsupported Operation {_token}") {
            this._opToken = _token;
            this._opAddr = _addr;
        }

        public string BadOpToken { get { return this._opToken; } }
        public ushort BadOpAddr { get { return this._opAddr; } }

    }

    public class BadSyntaxExcepiton : Exception {

        private string _badAsm;
        private ushort _opAddr;

        protected BadSyntaxExcepiton() : base() { }

        public BadSyntaxExcepiton(String _token, ushort _addr) : base(
            $"Assembly Code at Address {_addr:X} uses incorrect syntax. RTFM my guy!\n Offending Line: \"{_token}\"") {
            this._badAsm = _token;
            this._opAddr = _addr;
        }

        public string BadAsmCode { get { return this._badAsm; } }
        public ushort BadOpAddr { get { return this._opAddr; } }

    }

    public class LargeImmediateException : Exception {

        private string _badAsm;
        private ushort _opAddr;

        protected LargeImmediateException() : base() { }

        public LargeImmediateException(String _token, ushort _addr) : base(
            $"Assembly Code at Address {_addr:X} uses an immediate that is too large. RTFM my guy!\n Offending Line: \"{_token}\"") {
            this._badAsm = _token;
            this._opAddr = _addr;
        }

        public string BadAsmCode { get { return this._badAsm; } }
        public ushort BadOpAddr { get { return this._opAddr; } }

    }

    public class FarLabelException : Exception {

        private string _badAsm;
        private ushort _opAddr;
        private ushort _lblAddr;
        protected FarLabelException() : base() { }

        public FarLabelException(String _token, ushort _addr, ushort _laddr) : base(
            $"Assembly Code at Address {_addr:X} attempts to jump to label {_token} that is too far. RTFM my guy!\nLabel Location: {_laddr:X}"){
            this._badAsm = _token;
            this._opAddr = _addr;
            this._lblAddr = _laddr;
        }

        public string BadAsmCode { get { return this._badAsm; } }
        public ushort BadOpAddr { get { return this._opAddr; } }
        public ushort BadLblAddr { get { return this._lblAddr; } }

    }
}