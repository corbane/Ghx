

Call stack on load:

    [Open GH]
        - Constructor
        - Constructor
    [GH is open]
    [Open definition]
        - Constructor
        - Read
        - AddedToDocument
        - SolveInstance
    [Definition is loaded]

Call stack on SolveInstance:

CollectVolatileData_FromSources:

    - CollectVolatileData_FromSources
    [each params]
        - PostProcessData
        - OnVolatileDataCollected
    [according to the input access parameters]
    - SolveInstance * n

