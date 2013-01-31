﻿namespace FSCL.Compiler.Processors

open FSCL.Compiler
open Microsoft.FSharp.Quotations
open System.Collections.Generic
open System.Reflection

type ArrayAccessPrinter() =                 
    interface FunctionBodyPrettyPrintingProcessor with
        member this.Handle(expr, engine:FunctionPrettyPrintingStep) =
            match expr with
            | Patterns.Call(o, methodInfo, args) ->
                if methodInfo.DeclaringType.Name = "IntrinsicFunctions" then
                    let arrayName = engine.Continue(args.[0])
                    if methodInfo.Name = "GetArray" then
                        Some(arrayName + "[" + engine.Continue(args.[1]) + "]")
                    elif methodInfo.Name = "SetArray" then
                        Some(arrayName + "[" + engine.Continue(args.[1]) + "] = " + engine.Continue(args.[2]) + ";\n")
                    else
                        None
                else
                    None
            | _ ->
                None