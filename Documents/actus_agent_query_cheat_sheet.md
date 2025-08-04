
# 🧠 Actus Agent Query Cheat Sheet

A guide to example user questions and supported use cases handled by the Actus Agent system.

---

## 📺 Transcript Content Queries

### 📝 Summarization Requests
- “Summarize the Prime Minister's speech from yesterday on CNN.”
- “Give me a short overview of today's sports coverage on BBC.”
- “What were the top news topics from July 29th?”

### 😠 Emotion & Sentiment Analysis
- “What was the emotional tone of the panel discussion on Al Jazeera last night?”
- “Was there anger or sarcasm in the finance segment this morning?”

### 🏷️ Keyword & Entity Mentions
- “How many times was Trump mentioned today?”
- “Find all mentions of COVID-19 in the 6pm broadcast.”
- “Show every time the phrase ‘interest rate’ was said last week.”

### 💡 Topic & Theme Extraction
- “What efforts were discussed regarding Trump’s legal battles?”
- “List all topics related to climate change in the past 48 hours.”

---

## 🚨 Alerts & Event Queries

### 📅 Alerts Filtering
- “Show all alerts sent on July 30th across all channels.”
- “Give me alerts that contain both violence and keywords like ‘protest’ or ‘riot’.”
- “List alert messages triggered between 2pm and 4pm yesterday.”

### 🔄 Cross-Referencing Alerts & Transcripts
- “What was said in the transcript when the 'Breaking News' alert fired on Fox News at 3pm?”
- “Summarize the events that caused alerts on July 31st.”

---

## 🤖 Agent-Based Actions

### 🔍 Operational Agent Requests
- “Run face detection on Channel 12 from 8pm to 9pm.”
- “Detect all key phrases in the entertainment section last night.”
- “What keywords were extracted from today's 6 o’clock news?”

### 🤝 Multi-Agent Combinations
- “Detect faces and analyze emotions on last night’s BBC News.”
- “Send Gmail alerts if any transcript mentions a security breach.”

---

## 🗂️ Hybrid: Logs + Metadata Queries

### 📄 Logs & System Status
- “Find all error logs where alerts failed to send.”
- “List all log entries from AI jobs between 5pm–7pm.”
- “Did any logs show issues with CNN's transcription pipeline?”

### 📡 On-Demand Transcription
- “Can I transcribe CNN only when I ask for it?”
- “On-demand transcript: summarize what was said on Channel 10 at 10pm.”

---

## ⚙️ Dynamic vs Static Query Handling

| Type       | Description                               | Examples                                       |
|------------|-------------------------------------------|------------------------------------------------|
| Dynamic    | Freeform natural language input           | “What was said about interest rates today?”    |
| Static     | Predefined prompt templates or agents     | Agent: FaceDetection, Prompt: SummaryTemplate  |
| Hybrid     | GPT generates plan from user query        | “Analyze tone of Fox broadcast at 7pm” → plan  |

---

## 🧰 Useful Constructs for Dynamic Query Handling

- **Intent Detection** – Extract user intent from query
- **Semantic Embeddings** – Match similar content by meaning
- **Prompt Builder** – Create task-specific prompts dynamically
- **GPT Plan Generator** – Convert queries into multi-step execution plans
- **Agent Dispatcher** – Route tasks to appropriate AI agents
- **Vector Search** – Retrieve semantically similar transcript segments
- **Metadata Filters** – Limit results by channel, time, tags, etc.
- **On-demand Execution** – Only run heavy jobs when needed

---
