%using QUT.Gppg;
%namespace GardensPoint

%%

"print"       { return (int)Tokens.Print; }
"exit"        { return (int)Tokens.Exit; }
<<EOF>>       { return (int)Tokens.Eof; }
