# Data Flow & Privacy Architecture

MyCancerTeam is built with a strict **local-first, privacy-by-default** architecture. Because cancer care involves highly sensitive personal and medical information, the system is designed to ensure that **no documents or personal data are persisted in any cloud storage or database**.

The only data that leaves your local machine are the transient API calls required for the AI reasoning and optional web searches.

```mermaid
sequenceDiagram
    autonumber
    actor User
    participant LocalFS as Local File System<br/>(.local/ folders)
    participant App as MyCancerTeam App
    participant AzureOAI as Azure OpenAI
    participant WebSearch as Bing / SerpApi

    User->>LocalFS: Drops medical notes/reports (PDF/DOCX/TXT)
    App->>LocalFS: Scans files & extracts text locally
    Note over App, LocalFS: Extraction is strictly on-machine.<br/>Documents are NEVER uploaded to a cloud parser.

    User->>App: Asks question or starts workflow
    App->>AzureOAI: Sends extracted context + prompt for agent reasoning
    Note over App, AzureOAI: Transient API call only.<br/>Data is not persisted or used for model training.
    AzureOAI-->>App: Returns agent analysis

    opt If Research Agent needs external evidence
        App->>WebSearch: Sends targeted search queries
        WebSearch-->>App: Returns medical literature / trial results
    end

    App->>AzureOAI: Synthesizes final Multi-Disciplinary Team response
    AzureOAI-->>App: Returns final synthesis

    App->>LocalFS: Writes output to summary.md, notes.md & drafts locally
    App-->>User: Displays response in console
```

### Key Privacy Guarantees

1. **Local Text Extraction**: PDFs and Word documents are parsed locally on your machine using open-source libraries (`PdfPig` and `Open-XML-SDK`). Files are never uploaded to a cloud service to be read.
2. **No External Persistence**: There is no SQL database, CosmosDB, or cloud blob storage configured. All state, memory, drafts, `summary.md`, and `notes.md` are kept exclusively in the `.local/` folder on your hard drive.
3. **Zero Telemetry**: The application does not send telemetry, usage logs, or analytics to any central server.
4. **Transient AI Inference**: Data sent to Azure OpenAI is processed for inference only. Under standard Azure OpenAI enterprise terms, your prompts and data are not used to train foundational models.
5. **Transient Search Queries**: If configured, search queries sent to Bing or SerpApi are strictly for retrieving current medical literature or trials, and no personal medical records are included in the search payloads.