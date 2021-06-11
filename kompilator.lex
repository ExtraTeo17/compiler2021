%using QUT.Gppg;
%namespace GardensPoint

IntNumber   [0-9]+

%%

"program"		{ return (int)Tokens.Program; }
"{"				{ return (int)Tokens.OpenBracket; }
"}"				{ return (int)Tokens.CloseBracket; }
"write"			{ return (int)Tokens.Write; }
{IntNumber}		{ yylval.val=yytext; return (int)Tokens.IntNumber; }
" "				{ }
"\r"			{ }
"\t"			{ }
";"				{ return (int)Tokens.Semicolon; }
<<EOF>>			{ return (int)Tokens.Eof; }
