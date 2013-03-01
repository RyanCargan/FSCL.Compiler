﻿namespace FSCL.Compiler.Configuration

open System
open System.IO
open System.Reflection
open FSCL.Compiler
open System.Collections.Generic
open System.Xml
open System.Xml.Linq
open Microsoft.FSharp.Reflection

type CompilerSource =
| AssemblySource of Assembly
| FileSource of string

type StepConfiguration(i: string, t: Type, ?dependencies: string list, ?before: string list) =
    member val ID = i with get
    member val Dependencies = if dependencies.IsSome then dependencies.Value else [] with get
    member val Before = if before.IsSome then before.Value else [] with get
    member val Type = t with get
    member internal this.ToXml() =
        let el = new XElement(XName.Get(this.GetType().Name),
                    new XAttribute(XName.Get("ID"), this.ID),
                    new XAttribute(XName.Get("Type"), this.Type.FullName),
                    new XElement(XName.Get("Dependencies"),
                        Array.ofSeq(seq {
                            for item in this.Dependencies do
                                yield XElement(XName.Get("Item"), XAttribute(XName.Get("ID"), item))
                        })),
                    new XElement(XName.Get("Before"),
                        Array.ofSeq(seq {
                            for item in this.Before do
                                yield XElement(XName.Get("Item"), XAttribute(XName.Get("ID"), item))
                        })))
        el
    static member internal FromXml(el:XElement, a:Assembly) =
        let deps = List<string>()
        let bef = List<string>()
        for d in el.Elements(XName.Get("Dependencies")) do
            for item in d.Elements(XName.Get("Item")) do
                deps.Add(item.Attribute(XName.Get("ID")).Value)
        for d in el.Elements(XName.Get("Before")) do
            for item in d.Elements(XName.Get("Item")) do
                bef.Add(item.Attribute(XName.Get("ID")).Value)
        StepConfiguration(el.Attribute(XName.Get("ID")).Value, 
                          a.GetType(el.Attribute(XName.Get("Type")).Value), 
                          List.ofSeq deps, List.ofSeq bef)

    override this.Equals(o) =
        if o.GetType() <> this.GetType() then
            false
        else
            let oth = o :?> StepConfiguration
            let equal = ref(this.ID = oth.ID && this.Type = oth.Type && this.Dependencies.Length = oth.Dependencies.Length && this.Before.Length = oth.Before.Length)
            if !equal then
                List.iter (fun (item1:string) ->
                    equal := !equal && (List.tryFind(fun (item2:string) ->
                                        item1 = item2) oth.Dependencies).IsSome) this.Dependencies
            if !equal then
                List.iter (fun (item1:string) ->
                    equal := !equal && (List.tryFind(fun (item2:string) ->
                                        item1 = item2) oth.Before).IsSome) this.Before
            !equal

type StepProcessorConfiguration(i: string, s: string, t: Type, ?dependencies, ?before) =
    member val ID = i with get
    member val Step = s with get
    member val Dependencies = if dependencies.IsSome then dependencies.Value else [] with get
    member val Before = if before.IsSome then before.Value else [] with get
    member val Type = t with get

    member internal this.ToXml() =
        let el = new XElement(XName.Get(this.GetType().Name),
                    new XAttribute(XName.Get("ID"), this.ID),
                    new XAttribute(XName.Get("Step"), this.Step),
                    new XAttribute(XName.Get("Type"), this.Type.FullName),
                    new XElement(XName.Get("Dependencies"),
                        Array.ofSeq(seq {
                            for item in this.Dependencies do
                                yield XElement(XName.Get("Item"), XAttribute(XName.Get("ID"), item))
                        })),
                    new XElement(XName.Get("Before"),
                        Array.ofSeq(seq {
                            for item in this.Before do
                                yield XElement(XName.Get("Item"), XAttribute(XName.Get("ID"), item))
                        })))
        el
    static member internal FromXml(el:XElement, a:Assembly) =
        let deps = List<string>()
        let bef = List<string>()
        for d in el.Elements(XName.Get("Dependencies")) do
            for item in d.Elements(XName.Get("Item")) do
                deps.Add(item.Attribute(XName.Get("ID")).Value)
        for d in el.Elements(XName.Get("Before")) do
            for item in d.Elements(XName.Get("Item")) do
                bef.Add(item.Attribute(XName.Get("ID")).Value)
        StepProcessorConfiguration(el.Attribute(XName.Get("ID")).Value, 
                                   el.Attribute(XName.Get("Step")).Value, 
                                   a.GetType(el.Attribute(XName.Get("Type")).Value), 
                                   List.ofSeq deps, List.ofSeq bef)

type TypeHandlerConfiguration(i:string, t:Type, ?before: string list) =
    member val ID = i with get
    member val Before = if before.IsSome then before.Value else [] with get
    member val Type = t with get
    member internal this.ToXml() =
        let el = new XElement(
                    XName.Get(this.GetType().Name),
                    new XAttribute(XName.Get("ID"), this.ID),
                    new XAttribute(XName.Get("Type"), this.Type),
                    new XElement(XName.Get("Before"),
                        Array.ofSeq(seq {
                            for item in this.Before do
                                yield XElement(XName.Get("Item"), XAttribute(XName.Get("ID"), item))
                        })))
        el
    static member internal FromXml(el:XElement, a:Assembly) =
        let before = List<string>()
        for d in el.Elements(XName.Get("Before")) do
            for item in d.Elements(XName.Get("Item")) do
                before.Add(item.Attribute(XName.Get("ID")).Value)
        TypeHandlerConfiguration(el.Attribute(XName.Get("ID")).Value, a.GetType(el.Attribute(XName.Get("Type")).Value), List.ofSeq before)   

type SourceConfiguration(src: CompilerSource, 
                         th: TypeHandlerConfiguration list, 
                         s: StepConfiguration list, 
                         p: StepProcessorConfiguration list) = 
    member val Source = 
        match src with
        | AssemblySource(a) ->
            AssemblySource(a)
        | FileSource(s) ->
            if (Path.IsPathRooted(s)) then
                FileSource(s)
            else
               FileSource(Path.Combine(Directory.GetCurrentDirectory(), s))

    member val TypeHandlers = th with get
    member val Steps = s with get
    member val StepProcessors = p with get
    
    member this.IsExplicit
        with get() =
            (this.TypeHandlers.Length > 0) || (this.Steps.Length > 0) && (this.StepProcessors.Length > 0)
            
    member internal this.IsDefault 
        with get() =
            match this.Source with
            | FileSource(s) ->
                let a = Assembly.LoadFile(s)
                a.GetCustomAttribute<DefaultComponentAssembly>() <> null
            | AssemblySource(a) ->
                a.GetCustomAttribute<DefaultComponentAssembly>() <> null                

    new(src: CompilerSource) = 
        SourceConfiguration(src, [], [], [])

    member internal this.ToXml() =
        let el = new XElement(XName.Get(this.GetType().Name),
                    match this.Source with
                    | FileSource(s) ->
                        XAttribute(XName.Get("FileSource"), s)
                    | AssemblySource(s) ->
                        XAttribute(XName.Get("AssemblySource"), s.FullName))
        if (this.IsExplicit) then
            el.Add(new XElement(XName.Get("Components"),
                        new XElement(XName.Get("TypeHandlers"), 
                            Array.ofSeq(seq {
                                for item in this.TypeHandlers do
                                    yield item.ToXml()
                            })),
                        new XElement(XName.Get("Steps"), 
                            Array.ofSeq(seq {
                                for item in this.Steps do
                                    yield item.ToXml()
                            })),
                        new XElement(XName.Get("StepProcessors"),
                            Array.ofSeq(seq {
                                for item in this.StepProcessors do
                                    yield item.ToXml()
                            }))))
        el
        
    static member internal FromXml(el: XElement, srcRoot: string) =
        let mutable source = FileSource("")
        let mutable assembly = Assembly.GetExecutingAssembly()
        let root = srcRoot

        // Determine source type (file or assembly name) and load assembly
        if (el.Attribute(XName.Get("AssemblySource")) <> null) then
            assembly <- Assembly.Load(el.Attribute(XName.Get("AssemblySource")).Value)
            source <- AssemblySource(assembly)
        else            
            if Path.IsPathRooted(el.Attribute(XName.Get("FileSource")).Value) then
                source <- FileSource(el.Attribute(XName.Get("FileSource")).Value)
                assembly <- Assembly.LoadFile(el.Attribute(XName.Get("FileSource")).Value)
            else
                source <- FileSource(Path.Combine(root, el.Attribute(XName.Get("FileSource")).Value))
                assembly <- Assembly.LoadFile(Path.Combine(root, el.Attribute(XName.Get("FileSource")).Value))

        // Check if explicit or implicit
        if (el.Element(XName.Get("Components")) <> null) then
            let th = new List<TypeHandlerConfiguration>()
            let s = new List<StepConfiguration>()
            let sp = new List<StepProcessorConfiguration>()

            if (el.Element(XName.Get("Components")).Element(XName.Get("TypeHandlers")) <> null) then
                for item in el.Element(XName.Get("Components")).Element(XName.Get("TypeHandlers")).Elements() do
                    th.Add(TypeHandlerConfiguration.FromXml(item, assembly))
            if (el.Element(XName.Get("Components")).Element(XName.Get("Steps")) <> null) then
                for item in el.Element(XName.Get("Components")).Element(XName.Get("Steps")).Elements() do
                    s.Add(StepConfiguration.FromXml(item, assembly))
            if (el.Element(XName.Get("Components")).Element(XName.Get("StepsProcessors")) <> null) then
                for item in el.Element(XName.Get("Components")).Element(XName.Get("StepProcessors")).Elements() do
                    sp.Add(StepProcessorConfiguration.FromXml(item, assembly))
            
            let conf = new SourceConfiguration(source, List.ofSeq th, List.ofSeq s, List.ofSeq sp)
            conf
        else
            let conf = new SourceConfiguration(source)
            conf
            
    member internal this.MakeExplicit() =
        if not this.IsExplicit then            
            let assembly = 
                match this.Source with
                | AssemblySource(a) ->
                    a
                | FileSource(f) ->
                    Assembly.LoadFile(f)

            // Load assembly and analyze content
            let th = new List<TypeHandlerConfiguration>()
            let s = new List<StepConfiguration>()
            let sp = new List<StepProcessorConfiguration>()
            for t in assembly.GetTypes() do
                let dep = List<string>()
                let bef = List<string>()

                let thAttribute = t.GetCustomAttribute<TypeHandlerAttribute>()
                if thAttribute <> null then
                    for item in thAttribute.Before do
                        bef.Add(item)
                    th.Add(TypeHandlerConfiguration(thAttribute.ID, t, List.ofSeq bef))
                dep.Clear()
                bef.Clear()

                let sAttribute = t.GetCustomAttribute<StepAttribute>()
                if sAttribute <> null then
                    for item in sAttribute.Before do
                        bef.Add(item)
                    for item in sAttribute.Dependencies do
                        dep.Add(item)
                    s.Add(StepConfiguration(sAttribute.ID, t, List.ofSeq dep, List.ofSeq bef))
                dep.Clear()
                bef.Clear()
                        
                let spAttribute = t.GetCustomAttribute<StepProcessorAttribute>()
                if spAttribute <> null then
                    for item in spAttribute.Before do
                        bef.Add(item)
                    for item in spAttribute.Dependencies do
                        dep.Add(item)
                    sp.Add(StepProcessorConfiguration(spAttribute.ID, spAttribute.Step, t, List.ofSeq dep, List.ofSeq bef))

            // Return
            SourceConfiguration(this.Source, List.ofSeq th, List.ofSeq s, List.ofSeq sp)
        else
            this        

type CompilerConfiguration(defSteps, sources: SourceConfiguration list) =
    // Guarantee file sources absolute
    member val Sources = sources

    new(defSteps) =
        CompilerConfiguration(defSteps, [])
        
    member this.IsDefault 
        with get() =
            this.Sources |> List.map(fun (el:SourceConfiguration) -> el.IsDefault) |> List.reduce(fun (el1:bool) (el2:bool) -> el1 && el2)
            
    member this.LoadDefaultSteps 
        with get() = 
            defSteps && (not this.IsDefault)

    member internal this.ToXml() =
        let el = new XElement(XName.Get(this.GetType().Name),
                    new XAttribute(XName.Get("LoadDefaultSteps"), this.LoadDefaultSteps),
                    new XElement(XName.Get("Sources"),
                        Array.ofSeq(seq {
                            for item in this.Sources do
                                yield item.ToXml()
                        })))
        let doc = new XDocument(el)
        doc
        
    static member internal FromXml(doc: XDocument, srcRoot: string) =
        let sources = new List<SourceConfiguration>()
        let loadDefault = if(doc.Root.Attribute(XName.Get("LoadDefaultSteps")) <> null) then
                            bool.Parse(doc.Root.Attribute(XName.Get("LoadDefaultSteps")).Value)
                          else
                             false
        for s in doc.Root.Elements(XName.Get("Sources")) do
            for source in s.Elements() do
                sources.Add(SourceConfiguration.FromXml(source, srcRoot))
        
        CompilerConfiguration(loadDefault, List.ofSeq sources)
        
    member internal this.MakeExplicit() =
        let sources = new List<SourceConfiguration>()
        for item in this.Sources do
            // Filter out assembly with no components even if made explicit
            let exp = item.MakeExplicit()
            if exp.IsExplicit then
                sources.Add(item.MakeExplicit())        
        CompilerConfiguration(this.LoadDefaultSteps, List.ofSeq sources)
        
    member internal this.MergeDefault(def: CompilerConfiguration) =
        if this.LoadDefaultSteps then
            let sources = new List<SourceConfiguration>()
            for s in this.MakeExplicit().Sources do
                sources.Add(s)
            // Merge with default sources
            for s in def.MakeExplicit().Sources do
                sources.Add(s)
            CompilerConfiguration(false, List.ofSeq sources)
        else
            this.MakeExplicit()

                            
       
            

