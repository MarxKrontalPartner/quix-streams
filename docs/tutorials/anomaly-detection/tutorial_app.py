import os
import random
import time

from quixstreams import Application
from quixstreams.sources import Source


class TemperatureGenerator(Source):
    """
    What a Source is:
    A Quix Streams Source enables Applications to read data from something other than a Kafka topic.
    Basically, this generates JSON data that the StreamingDataFrame consumes directly.
    This simplifies the example by not having to run both a producer and Application.

    What it does:
    Generates temperature readings for three different machines.
    Machine ID's 0 and 1 are functioning normally; ID 2 is malfunctioning.
    It cycles through the ID's, generating random temperature changes of -1, 0, or 1.
    These probabilities change depending on current temperature and operation.
    """

    # Probabilities are % chance for [-1, 0, 1] change given a current temp in the
    # 40's, 50's, etc.
    probabilities_normal = {
        40: [0, 0, 100],
        50: [20, 30, 50],
        60: [30, 40, 30],
        70: [40, 50, 10],
        80: [80, 10, 10],
        90: [100, 0, 0],
    }

    probabilities_issue = {
        40: [0, 0, 100],
        50: [0, 10, 90],
        60: [5, 15, 80],
        70: [5, 20, 75],
        80: [5, 20, 75],
        90: [10, 20, 70],
    }

    def __init__(self):
        self.event_count = 0
        self.machine_temps = {0: 66, 1: 58, 2: 62}
        self.machine_types = {
            0: self.probabilities_normal,
            1: self.probabilities_normal,
            2: self.probabilities_issue,
        }
        super().__init__(name="temperature-event-generator")

    def update_machine_temp(self, machine_id):
        """
        Updates the temperature for a machine by -1, 0, or 1 based on its current temp.
        """
        self.machine_temps[machine_id] += random.choices(
            [-1, 0, 1],
            self.machine_types[machine_id][(self.machine_temps[machine_id] // 10) * 10],
        )[0]

    def generate_event(self):
        """
        Generate a temperature reading event for a Machine ID.

        It tracks/loops through the Machine ID's for equal number of readings.
        """
        machine_id = self.event_count % 3
        self.update_machine_temp(machine_id)
        event_out = {
            "key": str(machine_id),
            "value": {
                "Temperature_C": self.machine_temps[machine_id],
                "Timestamp": time.time_ns(),
            },
        }
        self.event_count += 1
        if self.machine_temps[machine_id] == 100:
            self.stop()
        return event_out

    def run(self):
        while self.running:
            event = self.serialize(**self.generate_event())
            print(f"producing event for MID {event.key}, {event.value}")
            self.produce(key=event.key, value=event.value)
            time.sleep(0.2)  # just to make things easier to follow along
        print("An alert should have been generated by now; stopping producing.")


# NOTE: A "metadata" function expects these 4 arguments regardless of use.
def should_alert(window_value: int, key, timestamp, headers):
    if window_value >= 90:
        print(f"Alerting for MID {key}: Average Temperature {window_value}")
        return True


def main():
    app = Application(
        broker_address=os.getenv("BROKER_ADDRESS", "localhost:9092"),
        consumer_group="temperature_alerter",
        auto_offset_reset="earliest",
    )
    alerts_topic = app.topic(name="temperature_alerts")

    # If reading from a Kafka topic, pass topic=<Topic> instead of a source
    sdf = app.dataframe(source=TemperatureGenerator())
    sdf = sdf.apply(lambda data: data["Temperature_C"])
    sdf = sdf.hopping_window(duration_ms=5000, step_ms=1000).mean().current()
    sdf = sdf.apply(lambda result: round(result["value"], 2)).filter(
        should_alert, metadata=True
    )
    sdf.to_topic(alerts_topic)

    app.run()


if __name__ == "__main__":
    main()
