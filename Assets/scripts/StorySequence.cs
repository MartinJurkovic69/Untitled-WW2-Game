using UnityEngine;
using System.Collections;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class StorySequence : MonoBehaviour
{
    [System.Serializable]
    public class TimedEvent
    {
        public float atSeconds;
        public UnityEvent action;
        [HideInInspector] public bool fired;
    }

    [System.Serializable]
    public class Step
    {
        public string name;
        public CrewStation station;
        public UnityEvent onBegin;
        public UnityEvent onEnd;
        public TimedEvent[] timedEvents;
    }
    public Step[] steps;

    [Header("Transitions")]
    public CanvasGroup fadeGroup; // will be changed for a transition animation in the future
    public float fadeTime = 0.6f;

    [Header("when the story ends")]
    public UnityEvent onSequenceFinished;
    public string endSceneName = "MainMenu";

    public int CurrentIndex => index;
    public CrewStation CurrentStation => current;
    private int index = -1;
    private CrewStation current;
    private float stepTime;
    private bool transitioning;

    private void Start()
    {
        if (fadeGroup != null) fadeGroup.alpha = 1f;
        StartCoroutine(Transition(0));
    }
    private void Update()
    {
        if (current == null || transitioning || PauseMenu.IsPaused) return;

        stepTime += Time.deltaTime;

        TimedEvent[] events = steps[index].timedEvents;
        if (events == null) return;

        foreach(TimedEvent e in events)
        {
            if (!e.fired && stepTime >= e.atSeconds)
            {
                e.fired = true;
                e.action.Invoke();
            }
        }
    }
    public void Next()
    {
        if (!transitioning) StartCoroutine(Transition(index + 1));
    }
    public void GoTo(int stepIndex)
    {
        if (!transitioning) StartCoroutine(Transition(stepIndex));
    }
    private void OnStationCompleted(CrewStation station)
    {
        if (station == current && !transitioning) Next();
    }
    private IEnumerator Transition(int next)
    {
        transitioning = true;

        if (current != null)
        {
            yield return Fade(1f);
            EndCurrent();
        }
        if (next >= steps.Length)
        {
            Finish();
            yield break;
        }
    }
    private void BeginStep(int next)
    {
        index = next;
        Step step = steps[index];

        current = step.station;
        stepTime = 0f;

        if (step.timedEvents != null)
            foreach (TimedEvent e in step.timedEvents) e.fired = false;
        
        if (current != null)
        {
            current.Completed += OnStationCompleted;
            current.Begin();
        }
        step.onBegin.Invoke();
    }
    private void EndCurrent()
    {
        if (current != null)
        {
            current.Completed -= OnStationCompleted;
            current.End();
        }
        steps[index].onEnd.Invoke();
        current = null;
    }
    private void Finish()
    {
        onSequenceFinished.Invoke();

        if (!string.IsNullOrEmpty(endSceneName))
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SceneManager.LoadScene(endSceneName);
        }

    }
    private IEnumerator Fade(float target)
    {
        if (fadeGroup == null || fadeTime <= 0f)
        {
            if (fadeGroup != null) fadeGroup.alpha = target;
            yield break;
        }
        float start = fadeGroup.alpha;
        for (float t = 0f; t < fadeTime; t += Time.unscaledDeltaTime)
        {
            fadeGroup.alpha = Mathf.Lerp(start, target, t / fadeTime);
            yield return null;
        }
        fadeGroup.alpha = target;
    }
}
