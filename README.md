![alt text](https://user-images.githubusercontent.com/52577119/107158852-c1f87680-699d-11eb-8dc8-f6085a02daa3.png)

# Overview
  * [About](#about)
  * [Installation](#installation)
  * [Usage](#usage)
  * [Examples](#examples)
  * [License](#license)

## About

 alm - is the simple functional programming language, with **self writed front-end** and using **MSIL like back-end**.
 
 The main purpose of this project is to **reach experience** in compiler design and finally **understand how really this all things work**, no more.
 
 I know that it's the thousandth implementation of compiler and it will be useless for most community, but i think that someday, for somebody, it may be useful.
 
 And so i leave it here, why not.

## Installation

 ### Requirements
  * **.NET 4+**
  
 Open the latest release (or simply [click](https://github.com/Almeswe/alm/releases/download/v.2.0.0/almc.v.2.0.0.zip) here) and download the zip with libs,tests and binary.
 
 Note that the 'libs' folder and the binary file(compiler) must located together, also i recommend you to not rename it, otherwise imports will work wrong.
  
 
## Usage
 Specifically for this task i created the '?' shell command which will show you all commands ant their definitions.
 
 But i also want to share you here some recomendations for comfortable use.
 
 The example algorithm for first use (with commands):
  * Open compiler's shell 
  * **> crfl "main.alm"**
  * **> opfl this** 
  * write some code
  * **> c 1 "test.exe"**
  * after this you may use only **'rec'** command for recompiling
     
 Other detailed information you may see after typing '?' command:
  * **> ?**
  
## Examples

  Before you start watching for code examples, i will say that the language has pretty easy C-like syntax.
  
  * ### Hello world!
  
      Without any words.

      ```cpp

      import io;

      func main() : integer
      {
         println("Hello world!");

         read("Press any key...");
         return 0;
      }

      ```
   * ### Simple array sort
   
      ```cpp
      
      import io;
      import random,array;
      
      func main() : integer
      {
          integer[] data = integer(20);
          for(integer i = 0; i < len(data);i+=1;)
              data[i] = randrange(1,15);
          data = sort(data);
          print_array(data);
          read("Press any key...");
          
          return 0;
      }

      func sort(integer[] data) : integer[]
      {
          integer i,j,buff;

          for (i = 0; i < len(data); i += 1;)
          {
              for (j = i + 1; j < len(data); j += 1;)
              {
                  if (data[j] < data[i])
                  {
                      buff = data[i];
                      data[i] = data[j];
                      data[j] = buff;
                  }
              }
          }
          return data;
      }

      ```
      
   * ### Arithmetical interpreter
   
      main.alm : 
   
      ```cpp
  
      import io;
      import "interpreter.alm";

      func main() : integer
      {
	         string input;
	         while(true)
	         {
		            input = read();
		            if (input == "quit")
                       break;
		            interpret(input);
	         }
	         return 0;
      }
  
      ```
      
      interpreter.alm :
      
      ```cpp
      
      import cast,array;
      import "lexer.alm","parser.alm";

      func interpret(string input) : void
      {
	         init_lexer(input);
	         string[] tokens = get_tokens();
             init_parser(tokens);
             float result = parse_expr();
	         if ((not PARSER_ERRORED) and not LEXER_ERRORED)
		            println("= " + tostrf(result));
      }
      
      ```
      
     lexer.alm : 
     
     ```cpp
     
     import chr;

     global string INPUT;
     global integer CH_INDEX;

     global boolean LEXER_ERRORED;

     global char CURR_CHAR,EOL = '\0';

     func init_lexer(string input) : void
     {
	        LEXER_ERRORED = false;
	        if (not check_input(input))
		           report_lexer_error("bad input");

	        CH_INDEX = -1;
	        INPUT = input + "\0";
     }

     func check_input(string input) : boolean
     {
	        for (integer i = 0; i < len(input); i += 1;)
		           if ((not digit(input[i])) and not op(input[i]))
			              return false;
	        return true;
     }

     func get_tokens() : string[]
     {
         next_char();

         string[] tokens = string(len(INPUT));
         integer token_count = 0;

         while (CURR_CHAR != EOL)
         {
             if (digit(CURR_CHAR))
             {
                 tokens[token_count] = get_number();
                 token_count += 1;
                 continue;
             }
             if (op(CURR_CHAR))
             {
                 tokens[token_count] = get_op();
                 token_count += 1;
                 continue;
             }
             else
                 next_char();
         }
         tokens = set_array_len(tokens,token_count);
         return tokens;
     }

     func get_op() : string
     {
         string op = tostrf(CURR_CHAR);
         next_char();
         return op;
     }

     func get_number() : string
     {
         string number = "";

         while(digit(CURR_CHAR) and CURR_CHAR != EOL)
         {
             number += tostrf(CURR_CHAR);
             next_char();
         }
         return number;	
     }

     func next_char() : void
     {
         if (len(INPUT)-1 > CH_INDEX)
         {
             CH_INDEX += 1;
             CURR_CHAR = INPUT[CH_INDEX];
         }	
         else 
             CURR_CHAR = EOL;
     }

     func digit(char ch) : boolean
     {
         if (IsDigit(ch))
             return true;
         return false;
     }
     
     func op(char ch) : boolean
     {
         char[] ops = char(6);
         ops[0] = '+';ops[1] = '*';
         ops[2] = '(';ops[3] = ')';
         ops[4] = '-';ops[5] = '/';

         if (in(ch,ops))
             return true;
         return false;
     }

     func report_lexer_error(string message) : void
     {
         LEXER_ERRORED = true;
         println("LEXER ERROR: " + message + ".");
     }
     
     ```
     parser.alm :
     
     ```cpp
     
     import io;

     global string[] TOKENS;
     global integer TK_INDEX;
     global string CURR_TOKEN;

     global boolean PARSER_ERRORED;

     func init_parser(string[] tokens) : void
     {
         TOKENS = tokens;
         TK_INDEX = -1;
         PARSER_ERRORED = false;
         next_token();
     }

     func next_token() : void
     {
         if (len(TOKENS)-1 > TK_INDEX)
         {
             TK_INDEX += 1;
             CURR_TOKEN = TOKENS[TK_INDEX];
         }
         else
         {
             TK_INDEX += 1;
             CURR_TOKEN = "\0";
         }
     }
     
     func parse_expr() : float
     {
         float result = 0;

         if (match("-"))
             result = parse_unary_minus();
         else 
             result = parse_mul();

         if (match("+"))
         {
             next_token();
             result += parse_expr();
             return result;
         }
         if (match("-"))
         {
             next_token();
             result -= parse_mul();

             if (match("+"))
             {
                 next_token();
                 result += parse_expr();
                 return result;
             }
             if (match("-"))
             {
                 result += parse_expr();
                 return result;
             }
             return result;
         }

         return result;
     }

     func parse_mul() : float
     {
         float result = parse_factor();
         if (match("*"))
         {
             next_token();
             result *= parse_mul();
             return result;
         }
         if (match("/"))
         {
             next_token();
             result /= parse_mul();
             return result;
         }
         return result;
     }

     func parse_factor() : float
     {
         if (num_token(CURR_TOKEN))
         {
             float num = tofloat(CURR_TOKEN);
             next_token();
             return num;
         }
         if (match("("))
             return parse_paren_expr();

         if (match("-"))
             return parse_unary_minus();

         report_parser_error("number, unary minus, or \'(\' expected");
         next_token();
         return 0;
     }

     func parse_paren_expr() : float
     {
         next_token();
         float result = parse_expr();
         if (not match(")"))
             report_parser_error("\')\' expected");
         return result;
     }

     func parse_unary_minus(): float
     {
         if (match("-",-1))
             report_parser_error("only one unary minus can be added in a row");
         next_token();
         return parse_mul() * -1;
     }

     func num_token(string token) : boolean
     {
         if (len(token) == 0)
             return false;
         for (integer i = 0; i < len(token); i += 1;)
             if (not digit(token[i]))
              return false;
         return true;
     }

     func match(string token,integer offset) : boolean
     {
         if (len(TOKENS)-1 < TK_INDEX+offset or TK_INDEX+offset < 0)
             return false;
         if (token == TOKENS[TK_INDEX+offset])
             return true;
         return false;
     }

     func match(string token) : boolean
     {
         return match(token,0);
     }

     func report_parser_error(string message) : void
     {
         PARSER_ERRORED = true;
         println("PARSER ERROR: " + message + ".");
     }
     
     ```
     
## License
   This project was released under [MIT](https://github.com/Almeswe/alm/blob/main/LICENSE) license.
