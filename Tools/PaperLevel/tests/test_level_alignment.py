import json
import unittest
from pathlib import Path

import cv2
import numpy as np


PROJECT_ROOT = Path(__file__).resolve().parents[3]
BACKGROUND_PATH = PROJECT_ROOT / "Assets/Resources/C1Levels/level1-background.png"
LEVEL_PATH = PROJECT_ROOT / "Assets/Resources/C1Levels/level1.json"


class LevelAlignmentTests(unittest.TestCase):
    def test_every_platform_follows_dark_ink_in_rectified_background(self):
        gray = cv2.imread(str(BACKGROUND_PATH), cv2.IMREAD_GRAYSCALE)
        level = json.loads(LEVEL_PATH.read_text(encoding="utf-8"))
        blackhat = cv2.morphologyEx(
            gray,
            cv2.MORPH_BLACKHAT,
            cv2.getStructuringElement(cv2.MORPH_RECT, (41, 9)),
        )

        scores = [self._ink_score(blackhat, platform) for platform in level["platforms"]]

        self.assertTrue(
            all(score >= 30 for score in scores),
            f"platforms must overlap photographed ink; blackhat scores={scores}",
        )

    @staticmethod
    def _ink_score(blackhat, platform):
        samples = []
        for progress in np.linspace(0.05, 0.95, 100):
            x = round(platform["x1"] + (platform["x2"] - platform["x1"]) * progress)
            y = round(platform["y1"] + (platform["y2"] - platform["y1"]) * progress)
            samples.append(blackhat[max(0, y - 4) : y + 5, x].max())
        return float(np.median(samples))


if __name__ == "__main__":
    unittest.main()
