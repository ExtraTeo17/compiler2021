%using QUT.Gppg;
%namespace GardensPoint



%%

"print"       { return (int)Tokens.Print; }
{IntNumber}   { yylval.val=yytext; return (int)Tokens.IntNumber; }
"exit"        { return (int)Tokens.Exit; }
"\r"          { return (int)Tokens.Endl; }
<<EOF>>       { return (int)Tokens.Eof; }
