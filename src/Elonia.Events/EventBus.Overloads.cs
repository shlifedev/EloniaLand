namespace Elonia.Events
{
    public partial class EventBus
    {
        public void Register<T1, T2>(IEventListener listener)
        {
            IEventListener<T1> listener1 = ValidateListener<T1>(listener);
            IEventListener<T2> listener2 = ValidateListener<T2>(listener);

            Register<T1>(listener1);
            Register<T2>(listener2);
        }

        public void Register<T1, T2, T3>(IEventListener listener)
        {
            IEventListener<T3> listener3 = ValidateListener<T3>(listener);

            Register<T1, T2>(listener);
            Register<T3>(listener3);
        }

        public void Register<T1, T2, T3, T4>(IEventListener listener)
        {
            IEventListener<T4> listener4 = ValidateListener<T4>(listener);

            Register<T1, T2, T3>(listener);
            Register<T4>(listener4);
        }

        public void Register<T1, T2, T3, T4, T5>(IEventListener listener)
        {
            IEventListener<T5> listener5 = ValidateListener<T5>(listener);

            Register<T1, T2, T3, T4>(listener);
            Register<T5>(listener5);
        }

        public void Register<T1, T2, T3, T4, T5, T6>(IEventListener listener)
        {
            IEventListener<T6> listener6 = ValidateListener<T6>(listener);

            Register<T1, T2, T3, T4, T5>(listener);
            Register<T6>(listener6);
        }

        public void Register<T1, T2, T3, T4, T5, T6, T7>(IEventListener listener)
        {
            IEventListener<T7> listener7 = ValidateListener<T7>(listener);

            Register<T1, T2, T3, T4, T5, T6>(listener);
            Register<T7>(listener7);
        }

        public void Register<T1, T2, T3, T4, T5, T6, T7, T8>(IEventListener listener)
        {
            IEventListener<T8> listener8 = ValidateListener<T8>(listener);

            Register<T1, T2, T3, T4, T5, T6, T7>(listener);
            Register<T8>(listener8);
        }

        public void Register<T1, T2, T3, T4, T5, T6, T7, T8, T9>(IEventListener listener)
        {
            IEventListener<T9> listener9 = ValidateListener<T9>(listener);

            Register<T1, T2, T3, T4, T5, T6, T7, T8>(listener);
            Register<T9>(listener9);
        }

        public void Register<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(IEventListener listener)
        {
            IEventListener<T10> listener10 = ValidateListener<T10>(listener);

            Register<T1, T2, T3, T4, T5, T6, T7, T8, T9>(listener);
            Register<T10>(listener10);
        }

        public void Register<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(IEventListener listener)
        {
            IEventListener<T11> listener11 = ValidateListener<T11>(listener);

            Register<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(listener);
            Register<T11>(listener11);
        }

        public void Register<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(IEventListener listener)
        {
            IEventListener<T12> listener12 = ValidateListener<T12>(listener);

            Register<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(listener);
            Register<T12>(listener12);
        }

        public void UnRegister<T1, T2>(IEventListener listener)
        {
            IEventListener<T1> listener1 = ValidateListener<T1>(listener);
            IEventListener<T2> listener2 = ValidateListener<T2>(listener);

            UnRegister<T1>(listener1);
            UnRegister<T2>(listener2);
        }

        public void UnRegister<T1, T2, T3>(IEventListener listener)
        {
            IEventListener<T3> listener3 = ValidateListener<T3>(listener);

            UnRegister<T1, T2>(listener);
            UnRegister<T3>(listener3);
        }

        public void UnRegister<T1, T2, T3, T4>(IEventListener listener)
        {
            IEventListener<T4> listener4 = ValidateListener<T4>(listener);

            UnRegister<T1, T2, T3>(listener);
            UnRegister<T4>(listener4);
        }

        public void UnRegister<T1, T2, T3, T4, T5>(IEventListener listener)
        {
            IEventListener<T5> listener5 = ValidateListener<T5>(listener);

            UnRegister<T1, T2, T3, T4>(listener);
            UnRegister<T5>(listener5);
        }

        public void UnRegister<T1, T2, T3, T4, T5, T6>(IEventListener listener)
        {
            IEventListener<T6> listener6 = ValidateListener<T6>(listener);

            UnRegister<T1, T2, T3, T4, T5>(listener);
            UnRegister<T6>(listener6);
        }

        public void UnRegister<T1, T2, T3, T4, T5, T6, T7>(IEventListener listener)
        {
            IEventListener<T7> listener7 = ValidateListener<T7>(listener);

            UnRegister<T1, T2, T3, T4, T5, T6>(listener);
            UnRegister<T7>(listener7);
        }

        public void UnRegister<T1, T2, T3, T4, T5, T6, T7, T8>(IEventListener listener)
        {
            IEventListener<T8> listener8 = ValidateListener<T8>(listener);

            UnRegister<T1, T2, T3, T4, T5, T6, T7>(listener);
            UnRegister<T8>(listener8);
        }

        public void UnRegister<T1, T2, T3, T4, T5, T6, T7, T8, T9>(IEventListener listener)
        {
            IEventListener<T9> listener9 = ValidateListener<T9>(listener);

            UnRegister<T1, T2, T3, T4, T5, T6, T7, T8>(listener);
            UnRegister<T9>(listener9);
        }

        public void UnRegister<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(IEventListener listener)
        {
            IEventListener<T10> listener10 = ValidateListener<T10>(listener);

            UnRegister<T1, T2, T3, T4, T5, T6, T7, T8, T9>(listener);
            UnRegister<T10>(listener10);
        }

        public void UnRegister<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(IEventListener listener)
        {
            IEventListener<T11> listener11 = ValidateListener<T11>(listener);

            UnRegister<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(listener);
            UnRegister<T11>(listener11);
        }

        public void UnRegister<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(IEventListener listener)
        {
            IEventListener<T12> listener12 = ValidateListener<T12>(listener);

            UnRegister<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(listener);
            UnRegister<T12>(listener12);
        }
    }
}
